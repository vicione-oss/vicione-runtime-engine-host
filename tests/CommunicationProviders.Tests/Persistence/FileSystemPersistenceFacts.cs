using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Testably.Abstractions.Testing;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.Persistence;

public sealed class FileSystemPersistence_Constructor
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_settings_instance_Async()
    {
        await using ConstructorProviderFactory providerFactory = new();

        var instance = providerFactory.CreateSettingsPersistence(new FileSystemCommunication { Directory = ".", }, new(), Substitute.For<ILoggerFactory>(), Substitute.For<INameResolver>(), AssemblyLoadContext.Default);

        instance.Should().NotBeNull();
    }

    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_variables_instance_Async()
    {
        await using ConstructorProviderFactory providerFactory = new();

        var instance = providerFactory.CreateVariablesPersistence(new FileSystemCommunication { Directory = ".", }, new(), Substitute.For<ILoggerFactory>(), Substitute.For<INameResolver>(), AssemblyLoadContext.Default);

        instance.Should().NotBeNull();
    }
}

public sealed class FileSystemPersistence_Connect
{
    [Fact]
    public async Task Creates_directory_Async()
    {
        var fileSystem = new MockFileSystem();
        FileSystemPersistenceContext context = new();
        var subDirectory = fileSystem.Path.Combine(context.Directory.Path, "subDir1", "subDir2");
        var persistence = new FileSystemPersistence(new() { Directory = subDirectory, }, context.Logger, context.NameResolver, AssemblyLoadContext.Default, context.Ids, fileSystem);
        fileSystem.Directory.Exists(subDirectory).Should().BeFalse();

        await persistence.ConnectAsync(CancellationToken.None);

        fileSystem.Directory.Exists(subDirectory).Should().BeTrue();
    }
}

public sealed class FileSystemPersistence_Load
{
    private readonly FileSystemPersistenceContext _context = new();

    [Theory]
    [InlineData("00000000-0000-0000-000000000001", 1, 2)]
    [InlineData("00000000-0000-0000-000000000001", 2, 1)]
    public async Task Returns_last_written_persistence_entry_Async(string id, short lastWrittenNo, short oldNo)
    {
        PersistenceEntry sampleData = new()
        {
            UniqueIdentifier = id,
            Time = new DateTime(2020, 9, 28, 12, 5, 14),
            Value = "Hello",
        };
        _context.Given_is_persistence_entry_file(id, oldNo);
        _context.Given_is_persistence_entry_file(id, lastWrittenNo, sampleData);

        _context.When_load_entries();

        await _context.Then_load_entries_succeeded_Async();
        _context.Then_entries_contain_single_entry(sampleData);
    }

    [Fact]
    public async Task Returns_nothing_if_no_file_exists_Async()
    {
        _context.When_load_entries();

        await _context.Then_load_entries_succeeded_Async();
        _context.Then_entries_are_empty();
    }

    [Fact]
    public async Task Throws_InvalidOperationException_if_content_is_empty_Async()
    {
        _context.Given_is_persistence_entry("00000000-0000-0000-000000000001", "test entry");
        _context.Given_is_persistence_entry_file("00000000-0000-0000-000000000001", 1);

        _context.When_load_entries();

        await _context.Then_aggregated_exception_is_thrown_Async<InvalidOperationException>("*00000000-0000-0000-000000000001*test entry*empty*");
    }

    [Fact]
    public async Task Continues_if_content_is_empty_in_one_file_Async()
    {
        _context.Given_is_persistence_entry("00000000-0000-0000-000000000001", "test entry");
        _context.Given_is_persistence_entry_file("00000000-0000-0000-000000000001", 2, new());
        _context.Given_is_persistence_entry_file("00000000-0000-0000-000000000001", 1);

        _context.When_load_entries();

        await _context.Then_load_entries_succeeded_Async();
        _context.Then_entries_contain_single_entry(new() { UniqueIdentifier = "00000000-0000-0000-000000000001", });
        _context.Then_aggregated_exception_is_logged<InvalidOperationException>(LogLevel.Warning, "*00000000-0000-0000-000000000001*test entry*empty*");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_if_content_has_wrong_format_Async()
    {
        _context.Given_is_persistence_entry("00000000-0000-0000-000000000001", "test entry");
        _context.Given_is_persistence_entry_file_with_invalid_content("00000000-0000-0000-000000000001", 1);

        _context.When_load_entries();

        await _context.Then_aggregated_exception_is_thrown_Async<InvalidOperationException>("*00000000-0000-0000-000000000001*test entry*incorrectly*formatted*");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_if_content_is_not_readable_Async()
    {
        _context.Given_is_persistence_entry("00000000-0000-0000-000000000001", "test entry");
        _context.Given_is_blocked_persistence_entry_file("00000000-0000-0000-000000000001", 1);

        _context.When_load_entries();

        await _context.Then_aggregated_exception_is_thrown_Async<InvalidOperationException>("*00000000-0000-0000-000000000001*test entry*not*read*");
    }

    [Fact]
    public async Task Does_not_read_files_from_subdirectories_Async()
    {
        PersistenceEntry sampleData = new()
        {
            UniqueIdentifier = "00000000-0000-0000-000000000001",
            Time = new DateTime(2020, 9, 28, 12, 5, 14),
            Value = "Hello",
        };
        _context.Given_is_persistence_entry_file("00000000-0000-0000-000000000001", 1, sampleData, "sub-directory");

        _context.When_load_entries();

        await _context.Then_load_entries_succeeded_Async();
        _context.Then_entries_are_empty();
    }

    [Fact]
    public async Task Does_not_read_foreign_entries_Async()
    {
        _context.Given_is_persistence_entry_file("00000000-0000-0000-000000000001", 1);
        _context.Given_is_no_relevant_persistence_entries();

        _context.When_load_entries();

        await _context.Then_load_entries_succeeded_Async();
        _context.Then_entries_are_empty();
    }
}

public sealed class FileSystemPersistence_Save
{
    private readonly FileSystemPersistenceContext _context = new();

    [Fact]
    public async Task Creates_new_file_for_unsaved_entry_Async()
    {
        PersistenceEntry entry = new()
        {
            UniqueIdentifier = "00000000-0000-0000-000000000001",
            Time = new DateTime(2020, 9, 28, 12, 5, 14),
            Value = "Hello",
        };

        await _context.When_save_entries_Async([entry,]);

        _context.Then_directory_have_files(1);
        _context.Then_directory_contain_file(entry.UniqueIdentifier, 1, entry);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public async Task Writes_persistence_entry_alternating_Async(short newNo, short oldNo)
    {
        PersistenceEntry newEntry = new()
        {
            UniqueIdentifier = "00000000-0000-0000-000000000001",
            Time = new DateTime(2020, 9, 28, 12, 5, 14),
            Value = "Hello",
        };
        PersistenceEntry oldEntry = new()
        {
            UniqueIdentifier = "00000000-0000-0000-000000000001",
            Time = new DateTime(2020, 9, 28, 12, 5, 14),
            Value = "World",
        };
        _context.Given_is_persistence_entry_file("00000000-0000-0000-000000000001", oldNo, oldEntry);

        await _context.When_save_entries_Async([newEntry,]);

        _context.Then_directory_have_files(2);
        _context.Then_directory_contain_file("00000000-0000-0000-000000000001", newNo, newEntry);
    }

    [Fact]
    public async Task Overwrites_existing_file_for_already_saved_entry_Async()
    {
        PersistenceEntry entry = new()
        {
            UniqueIdentifier = "00000000-0000-0000-000000000001",
            Time = new DateTime(2020, 9, 28, 12, 5, 14),
            Value = "Hello",
        };
        _context.Given_is_persistence_entry_file("00000000-0000-0000-000000000001", 1);
        _context.Given_is_persistence_entry_file("00000000-0000-0000-000000000001", 2);

        await _context.When_save_entries_Async([entry,]);

        _context.Then_directory_have_files(2);
        _context.Then_directory_contain_file("00000000-0000-0000-000000000001", 1, entry);
    }
}


internal sealed class FileSystemPersistenceContext
{
    internal DirectoryMock Directory { get; } = new();
    internal FileSystemPersistence Persistence { get; }
    internal TestLogger<FileSystemPersistence> Logger { get; } = new();
    internal INameResolver NameResolver { get; } = Substitute.For<INameResolver>();
    internal List<string> Ids { get; } = [];
    private IReadOnlyCollection<PersistenceEntry> _entries = [];
    private Func<Task<IReadOnlyCollection<PersistenceEntry>>> _loadActAsync = () => Task.FromResult<IReadOnlyCollection<PersistenceEntry>>([]);

    internal FileSystemPersistenceContext()
        => Persistence = new(new() { Directory = Directory.Path, }, Logger, NameResolver, AssemblyLoadContext.Default, Ids, Directory.FileSystem);

    internal void Given_is_persistence_entry_file_with_invalid_content(string id, short number)
    {
        var filename = Directory.CreateFile($"{id}@{number}");
        Ids.Add(id);
        Directory.FileSystem.File.WriteAllText(filename, "{");
    }

    internal FileSystemStream Given_is_blocked_persistence_entry_file(string id, short number)
    {
        var filename = Directory.CreateFile($"{id}@{number}");
        Ids.Add(id);
        return Directory.FileSystem.File.Create(filename);
    }

    internal void Given_is_persistence_entry_file(string id, short number, PersistenceEntry? content = null, params string[] pathParts)
    {
        var filename = Directory.CreateFile([.. pathParts, .. new[] { $"{id}@{number}" }]);
        Directory.FileSystem.File.WriteAllText(filename, content is null ? "null" : JsonSerializer.Serialize(content));
        Ids.Add(id);
    }

    internal void Given_is_persistence_entry(string id, string description)
        => NameResolver.ResolveName(id).Returns(description);

    internal void Given_is_no_relevant_persistence_entries()
        => Ids.Clear();

    internal void When_load_entries()
        => _loadActAsync = () => Persistence.LoadAsync(CancellationToken.None);

    internal Task When_save_entries_Async(IEnumerable<PersistenceEntry> entries)
        => Persistence.SaveAsync(entries, CancellationToken.None);

    internal void Then_entries_contain_single_entry(PersistenceEntry entry)
        => _entries.Should().ContainSingle().Which.Should().BeEquivalentTo(entry);

    internal void Then_entries_are_empty()
        => _entries.Should().BeEmpty();

    internal async Task Then_load_entries_succeeded_Async()
        => _entries = await _loadActAsync();

    internal async Task Then_aggregated_exception_is_thrown_Async<T>(string message) where T : Exception
        => (await _loadActAsync.Should().ThrowAsync<AggregateException>()).WithInnerException<T>().WithMessage(message);

    internal void Then_aggregated_exception_is_logged<T>(LogLevel logLevel, string message) where T : Exception
    {
        var entry = Logger.Entries.First(e => e.Exception?.InnerException?.GetType() == typeof(T));
        entry.LogLevel.Should().Be(logLevel);
        entry.Exception!.InnerException!.Message.Should().MatchEquivalentOf(message);
    }

    internal void Then_directory_have_files(int count)
        => Directory.FileSystem.Directory.GetFiles(Directory.Path).Should().HaveCount(count);

    internal void Then_directory_contain_file(string id, short number, PersistenceEntry content)
    {
        var filename = Directory.FileSystem.File.ReadAllText(Path.Combine(Directory.Path, $"{id}@{number}"));
        var data = JsonSerializer.Deserialize<PersistenceEntry>(filename);

        data.Should().BeEquivalentTo(content, o => o.Excluding(e => e.UniqueIdentifier));
    }
}
