using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.ManagedEngine.EngineDeployment;

public class DirectoryDeployment_ConfigureDeployment
{
    private const string DeploymentIdentifier = "engine";

    [Fact]
    public async Task Writes_deploy_parameter()
    {
        using var directory = new TemporaryDirectory();
        DeployParameter deployParameter = new()
        {
            CycleTime = 100,
            EngineChainIndex = 10,
            PackageReferences =
            [
                new()
                {
                    Name = "Package",
                    Version = "1.0.0",
                }
            ]
        };

        await DirectoryDeployment.ConfigureDeployment(directory.Path, DeploymentIdentifier, string.Empty,
            deployParameter, directory.FileSystem, CancellationToken.None);

        (await ReadCompressedJsonFile(FileNameHelper.GetDeployParameterFileName(Path.Combine(directory.Path, DeploymentIdentifier), directory.FileSystem), directory.FileSystem))
            .Should().Be(@"{""PackageReferences"":[{""Name"":""Package"",""Version"":""1.0.0""}],""EngineChainIndex"":10,""CycleTime"":100}");
    }

    [Fact]
    public async Task Writes_start_parameter()
    {
        using var directory = new TemporaryDirectory();

        await DirectoryDeployment.ConfigureDeployment(directory.Path, DeploymentIdentifier, "{}",
            new(), directory.FileSystem, CancellationToken.None);

        (await ReadCompressedJsonFile(FileNameHelper.GetStartParameterFileName(directory.FileSystem.Path.Combine(directory.Path, DeploymentIdentifier), directory.FileSystem), directory.FileSystem)).Should().Be("{}");
    }

    private static async Task<string> ReadCompressedJsonFile(string path, IFileSystem fileSystem)
    {
        await using var file = fileSystem.FileStream.New(path, new FileStreamOptions()
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
        });
        await using GZipStream gzip = new(file, CompressionMode.Decompress);
        using StreamReader reader = new(gzip);
        return await reader.ReadToEndAsync();
    }
}

public class DirectoryDeployment_ReadDeployParameter
{
    private const string DeploymentIdentifier = "engine";

    [Fact]
    public async Task Returns_data_from_file()
    {
        using TemporaryDirectoryDeployment deployment = new(DeploymentIdentifier, string.Empty);
        DeployParameter expected = new() { CycleTime = 123, };
        deployment.WriteDeployParameter(expected);

        var actual = await DirectoryDeployment.ReadDeployParameter(deployment.Path, DeploymentIdentifier, deployment.FileSystem, default);

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Can_handle_missing_files()
    {
        using TemporaryDirectoryDeployment deployment = new(DeploymentIdentifier, string.Empty);

        var call = FluentActions.Awaiting(() => DirectoryDeployment.ReadDeployParameter(deployment.Path, DeploymentIdentifier, deployment.FileSystem, default));

        await call.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not*find*deploy*parameter*json.gz'*");
    }

    [Fact]
    public async Task Can_handle_read_failures()
    {
        using TemporaryDirectoryDeployment deployment = new(DeploymentIdentifier, string.Empty);
        deployment.WriteDeployParameter();

        using var _ = deployment.FileSystem.File.Open(deployment.DeployParameterFile, FileMode.Open, FileAccess.ReadWrite);

        var call = FluentActions.Awaiting(() => DirectoryDeployment.ReadDeployParameter(deployment.Path, DeploymentIdentifier, deployment.FileSystem, default));

        await call.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*not*read*deploy*parameter*json.gz'*");
    }
}

public class DirectoryDeployment_ReadStartParameter
{
    private const string DeploymentIdentifier = "engine";

    [Fact]
    public async Task Returns_data_from_file()
    {
        using TemporaryDirectoryDeployment deployment = new(DeploymentIdentifier, string.Empty);
        var engineName = nameof(Returns_data_from_file);
        deployment.WriteStartParameter(new() { Engine = new() { Name = engineName, } });

        var startParameter = await DirectoryDeployment.ReadStartParameter(deployment.Path, DeploymentIdentifier, deployment.FileSystem, default);

        startParameter.Should().Contain(engineName);
    }

    [Fact]
    public async Task Can_handle_missing_files()
    {
        using TemporaryDirectoryDeployment deployment = new(DeploymentIdentifier, string.Empty);

        var call = FluentActions.Awaiting(() => DirectoryDeployment.ReadStartParameter(deployment.Path, DeploymentIdentifier, deployment.FileSystem, default));

        await call.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not*find*start*parameter*json.gz'*");
    }

    [Fact]
    public async Task Can_handle_read_failures()
    {
        using TemporaryDirectoryDeployment deployment = new(DeploymentIdentifier, string.Empty);
        deployment.WriteStartParameter();
        using var _ = deployment.FileSystem.File.Open(deployment.StartParameterFile, FileMode.Open, FileAccess.ReadWrite);

        var call = FluentActions.Awaiting(() => DirectoryDeployment.ReadStartParameter(deployment.Path, DeploymentIdentifier, deployment.FileSystem, default));

        await call.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*not*read*start*parameter*json.gz'*");
    }
}

public class DirectoryDeployment_DeleteDeployment
{
    private const string DeploymentIdentifier = "engine";

    [Fact]
    public void Removes_the_entire_deployment()
    {
        using var directory = new TemporaryDirectory();
        directory.CreateDirectory(DeploymentIdentifier, "sampleDir");
        directory.CreateFile(DeploymentIdentifier, "foo", "bar");

        DirectoryDeployment.Delete(directory.Path, DeploymentIdentifier, directory.FileSystem);

        directory.FileSystem.Directory.Exists(Path.Combine(directory.Path, DeploymentIdentifier)).Should().BeFalse();
    }
}
