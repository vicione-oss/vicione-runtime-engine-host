using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Testably.Abstractions;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine.Persistence;

public sealed class FileSystemPersistence : ISettingsPersistence<FileSystemCommunication>, IVariablesPersistence<FileSystemCommunication>
{
    private readonly IFileSystem _fileSystem;
    private readonly IDirectoryInfo _directory;
    private readonly ILogger<FileSystemPersistence> _logger;
    private readonly INameResolver _nameResolver;
    private readonly List<string> _relevantEntries;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly FileStreamOptions _loadFileOptions = new()
    {
        Access = FileAccess.Read,
        Mode = FileMode.Open,
        Share = FileShare.ReadWrite,
        Options = FileOptions.Asynchronous,
    };
    private readonly FileStreamOptions _saveFileOptions = new()
    {
        Access = FileAccess.Write,
        Mode = FileMode.Create,
        Share = FileShare.Read,
        Options = FileOptions.Asynchronous,
    };

    public FileSystemPersistence(FileSystemCommunication communication, ILogger<FileSystemPersistence> logger, INameResolver nameResolver, AssemblyLoadContext assemblyLoadContext, SettingsPersistenceOptions options)
        : this(communication, logger, nameResolver, assemblyLoadContext, options.SettingIds, new RealFileSystem())
    { }

    public FileSystemPersistence(FileSystemCommunication communication, ILogger<FileSystemPersistence> logger, INameResolver nameResolver, AssemblyLoadContext assemblyLoadContext, VariablesPersistenceOptions options)
        : this(communication, logger, nameResolver, assemblyLoadContext, options.VariableIds, new RealFileSystem())
    { }

    internal FileSystemPersistence(FileSystemCommunication communication, ILogger<FileSystemPersistence> logger, INameResolver nameResolver, AssemblyLoadContext assemblyLoadContext, List<string> relevantEntries, IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _directory = _fileSystem.DirectoryInfo.New(communication.Directory);
        _logger = logger;
        _nameResolver = nameResolver;
        _relevantEntries = relevantEntries;
        _serializerOptions = JsonSetup.CreatePreserveTypeOptions(assemblyLoadContext);
    }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (!_directory.Exists)
            _directory.Create();
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<IReadOnlyCollection<PersistenceEntry>> LoadAsync(CancellationToken cancellationToken)
    {
        var files = GetFiles();
        return await Load(files, cancellationToken);
    }

    private Dictionary<string, IFileInfo[]> GetFiles()
        => _directory
            .GetFiles("*@?")
            .GroupBy(f => f.Name.Split("@")[0])
            .Where(g => _relevantEntries.Contains(g.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(g => g.Key, g => g.ToArray());

    private async Task<List<PersistenceEntry>> Load(Dictionary<string, IFileInfo[]> persistenceFiles, CancellationToken cancellationToken)
    {
        List<PersistenceEntry> persistenceEntries = [];
        List<Exception> exceptions = new(2);
        foreach (var (uniqueIdentifier, files) in persistenceFiles)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (!files.Any(f => f.Exists))
                continue;

            var data = await TryReadPersistenceEntry(uniqueIdentifier, files, exceptions, cancellationToken)
                ?? throw new AggregateException($"Persistence-Entry '{uniqueIdentifier}' ({_nameResolver.ResolveName(uniqueIdentifier)}) cannot be read.", exceptions);

            if (exceptions.Count != 0)
                _logger.ReadEntriesWithSomeTrouble(uniqueIdentifier, _nameResolver.ResolveName(uniqueIdentifier), new AggregateException(exceptions));
            exceptions.Clear();

            persistenceEntries.Add(new PersistenceEntry
            {
                UniqueIdentifier = uniqueIdentifier,
                Time = data.Time,
                Value = data.Value,
            });

            _logger.PersistenceEntryLoaded(uniqueIdentifier, _nameResolver.ResolveName(uniqueIdentifier));
        }
        return persistenceEntries;
    }

    private async Task<Data?> TryReadPersistenceEntry(string uniqueIdentifier, IFileInfo[] files, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        Data? data = default;

        foreach (var file in files.OrderByDescending(f => f.LastWriteTimeUtc))
        {
            try
            {
                await using var stream = _fileSystem.FileStream.New(file.FullName, _loadFileOptions);
                data = await JsonSerializer.DeserializeAsync<Data>(stream, _serializerOptions, cancellationToken);

                if (data is null)
                    exceptions.Add(new InvalidOperationException($"Persistence-Entry in file '{file.Name}' ({_nameResolver.ResolveName(uniqueIdentifier)}) is empty."));
                else
                    break;
            }
            catch (JsonException ex)
            {
                exceptions.Add(new InvalidOperationException($"Persistence-Entry in file '{file.Name}' ({_nameResolver.ResolveName(uniqueIdentifier)}) is incorrectly formatted.", ex));
            }
            catch (IOException ex)
            {
                exceptions.Add(new InvalidOperationException($"Persistence-Entry in file '{file.Name}' ({_nameResolver.ResolveName(uniqueIdentifier)}) could not be read.", ex));
            }
        }

        return data;
    }

    public async Task SaveAsync(IEnumerable<PersistenceEntry> persistenceEntries, CancellationToken cancellationToken)
    {
        foreach (var persistenceEntry in persistenceEntries)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            await Save(persistenceEntry, cancellationToken);
        }
    }

    private async Task Save(PersistenceEntry persistenceEntry, CancellationToken cancellationToken)
    {
        var filename = GetNextFilename();

        try
        {
            await Save(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.PersistenceEntryNotSaved(filename, ex);
        }

        async Task Save(CancellationToken cancellationToken)
        {
            await using var stream = _fileSystem.FileStream.New(_fileSystem.Path.Combine(_directory.FullName, filename), _saveFileOptions);
            await JsonSerializer.SerializeAsync(stream, new Data { Time = persistenceEntry.Time, Value = persistenceEntry.Value, },
                _serializerOptions, cancellationToken);
            _logger.PersistenceEntrySaved(persistenceEntry.UniqueIdentifier, _nameResolver.ResolveName(persistenceEntry.UniqueIdentifier));
        }

        string GetNextFilename()
        {
            var lastFile = _directory
                .GetFiles($"{persistenceEntry.UniqueIdentifier}@?")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .DefaultIfEmpty(_fileSystem.FileInfo.New("2"))
                .First();

            if (lastFile.Name.EndsWith('1'))
                return $"{persistenceEntry.UniqueIdentifier}@2";
            else
                return $"{persistenceEntry.UniqueIdentifier}@1";
        }
    }

    internal sealed class Data
    {
        public DateTime Time { get; set; }

        [JsonConverter(typeof(TypeNameHandlingConverter))]
        public object? Value { get; set; }
    }
}
