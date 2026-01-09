using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Text.Json;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.EngineDeployment;

namespace ViciOne.ManagedEngine;

internal sealed class TemporaryDirectoryDeployment : IDisposable
{
    private readonly string _contextIdentifier;
    private readonly TemporaryDirectory _directory;
    private readonly string _deploymentPath;

    internal string Path => _directory.Path;
    internal IFileSystem FileSystem => _directory.FileSystem;
    internal string StartParameterFile { get; }
    internal string DeployParameterFile { get; }

    internal TemporaryDirectoryDeployment()
        : this(string.Empty, string.Empty)
    {
    }

    public TemporaryDirectoryDeployment(string deploymentIdentifier, string contextIdentifier)
    {
        _contextIdentifier = contextIdentifier;
        _directory = new TemporaryDirectory();
        _deploymentPath = _directory.CreateDirectory(deploymentIdentifier);
        StartParameterFile = FileNameHelper.GetStartParameterFileName(_deploymentPath, _directory.FileSystem);
        DeployParameterFile = FileNameHelper.GetDeployParameterFileName(_deploymentPath, _directory.FileSystem);
    }

    internal void RemoveDeployment() => Directory.Delete(_deploymentPath, true);

    internal void WriteStartParameter() => WriteStartParameter(new());
    internal void WriteStartParameter(StartParameter startParameter) => Write(StartParameterFile, startParameter, _directory.FileSystem, JsonSetup.CreatePreserveTypeOptions());

    internal void WriteDeployParameter() => WriteDeployParameter(new());
    internal void WriteDeployParameter(DeployParameter deployParameter) => Write(DeployParameterFile, deployParameter, _directory.FileSystem);

    internal void PlaceAssemblies() => _directory.CreateDirectory(_contextIdentifier);

    private static void Write<T>(string fileName, T instance, IFileSystem fileSystem, JsonSerializerOptions? options = default)
    {
        using var file = fileSystem.File.Create(fileName);
        using GZipStream gzip = new(file, CompressionMode.Compress);
        JsonSerializer.Serialize(gzip, instance, options);
    }

    public void Dispose() => _directory.Dispose();
}
