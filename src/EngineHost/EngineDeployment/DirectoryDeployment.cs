using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.EngineDeployment;

internal static class DirectoryDeployment
{
    private static readonly FileStreamOptions s_writeOptions = new()
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write,
        Share = FileShare.Read,
        Options = FileOptions.Asynchronous,
    };

    private static readonly FileStreamOptions s_readOptions = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
    };

    internal static IReadOnlyCollection<string> GetDeployments(string directory, IFileSystem fileSystem)
        => [.. fileSystem.Directory.EnumerateDirectories(directory, "????????-????-????-????-????????????", SearchOption.TopDirectoryOnly).Select(d => fileSystem.Path.GetFileName(d))];

    internal static async Task ConfigureDeployment(string directory, string deploymentIdentifier,
        [StringSyntax(StringSyntaxAttribute.Json)] string startParameter, DeployParameter deployParameter, IFileSystem fileSystem, CancellationToken cancellationToken)
    {
        var deploymentPath = GetPath(directory, deploymentIdentifier, fileSystem);

        fileSystem.Directory.CreateDirectory(deploymentPath);

        await PlaceStartParameter(startParameter, deploymentPath, fileSystem, cancellationToken).ConfigureAwait(false);
        await PlaceDeployParameter(deployParameter, deploymentPath, fileSystem, cancellationToken).ConfigureAwait(false);
    }

    private static async Task PlaceStartParameter([StringSyntax(StringSyntaxAttribute.Json)] string startParameter, string deploymentPath, IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var fileName = FileNameHelper.GetStartParameterFileName(deploymentPath, fileSystem);
        await using var file = fileSystem.FileStream.New(fileName, s_writeOptions);
        await using GZipStream gzip = new(file, CompressionMode.Compress);
        await using StreamWriter writer = new(gzip);
        await writer.WriteAsync(startParameter.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private static async Task PlaceDeployParameter(DeployParameter deployParameter, string deploymentPath, IFileSystem fileSystem, CancellationToken cancellationToken)
    {
        var fileName = FileNameHelper.GetDeployParameterFileName(deploymentPath, fileSystem);
        await using var file = fileSystem.FileStream.New(fileName, s_writeOptions);
        await using GZipStream gzip = new(file, CompressionMode.Compress);
        await JsonSerializer.SerializeAsync(gzip, deployParameter, cancellationToken: cancellationToken);
    }

    internal static async Task<DeployParameter> ReadDeployParameter(string directory, string deploymentIdentifier, IFileSystem fileSystem, CancellationToken cancellationToken)
    {
        var deploymentPath = GetPath(directory, deploymentIdentifier, fileSystem);
        var fileName = FileNameHelper.GetDeployParameterFileName(deploymentPath, fileSystem);
        if (!fileSystem.File.Exists(fileName))
            throw new InvalidOperationException($"Cannot find deploy parameter file '{fileName}'.");
        try
        {
            await using var file = fileSystem.FileStream.New(fileName, s_readOptions);
            await using GZipStream gzip = new(file, CompressionMode.Decompress);
            return await JsonSerializer.DeserializeAsync<DeployParameter>(gzip, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("JSON file content cannot be null.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot read deploy parameter from '{fileName}'.", ex);
        }
    }

    internal static async Task<string> ReadStartParameter(string directory, string deploymentIdentifier, IFileSystem fileSystem, CancellationToken cancellationToken)
    {
        var deploymentPath = GetPath(directory, deploymentIdentifier, fileSystem);
        var fileName = FileNameHelper.GetStartParameterFileName(deploymentPath, fileSystem);
        if (!fileSystem.File.Exists(fileName))
            throw new InvalidOperationException($"Cannot find start parameter file '{fileName}'.");
        try
        {
            await using var file = fileSystem.FileStream.New(fileName, s_readOptions);
            await using GZipStream gzip = new(file, CompressionMode.Decompress);
            using StreamReader reader = new(gzip);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot read start parameter from '{fileName}'.", ex);
        }
    }

    internal static void Delete(string directory, string deploymentIdentifier, IFileSystem fileSystem)
    {
        var path = fileSystem.DirectoryInfo.New(GetPath(directory, deploymentIdentifier, fileSystem));
        if (path.Exists)
            path.Delete(true);
    }

    private static string GetPath(string directory, string deploymentIdentifier, IFileSystem fileSystem) => fileSystem.Path.Combine(directory, deploymentIdentifier);
}
