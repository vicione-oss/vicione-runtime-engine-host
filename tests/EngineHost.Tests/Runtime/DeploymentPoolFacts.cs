using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.EngineDeployment;
using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

public sealed class CreateDeploymentAsync : IDisposable
{
    private readonly DeploymentPoolContext _context = new();
    private readonly JsonSerializerOptions _serializerOptions = JsonSetup.CreatePreserveTypeOptions();

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Deploys_engine_Async()
    {
        AssemblyLoadContext context = new("context");
        await _context.EnginePool.CreateDeploymentAsync("1", context, JsonSerializer.Serialize(DeploymentPoolContext.StartParameter, _serializerOptions), new(), CancellationToken.None);

        _context.EnginePool._deployments.Should().ContainSingle();
        _context.Deployment.FileSystem.Directory.EnumerateDirectories(_context.Config.Value.DeploymentsDirectory, "*", SearchOption.AllDirectories).Select(d => new DirectoryInfo(d).Name)
            .Should().Contain("1");
    }

    [Fact]
    public async Task Can_handle_already_existing_deployment_Async()
    {
        AssemblyLoadContext context = new("context");
        var startParameter = JsonSerializer.Serialize(DeploymentPoolContext.StartParameter, _serializerOptions);
        await _context.EnginePool.CreateDeploymentAsync("1", context, startParameter, new(), CancellationToken.None);

        var act = FluentActions.Invoking(() => _context.EnginePool.CreateDeploymentAsync("1", context, startParameter, new(), CancellationToken.None));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not*store*deployment*1*");
        _context.EnginePool._deployments.Should().ContainSingle();
    }
}

public sealed class RecoverDeploymentAsync : IDisposable
{
    private readonly DeploymentPoolContext _context = new();
    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Recognizes_missing_start_parameter_file_Async()
    {
        AssemblyLoadContext context = new("context");

        var act = FluentActions.Awaiting(() => _context.EnginePool.RecoverDeploymentAsync("1", context, CancellationToken.None));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not*find*start*parameter*");
    }

    [Fact]
    public async Task Can_read_start_parameter_from_disk_Async()
    {
        AssemblyLoadContext context = new("context");
        Dataflow dataflow = new()
        {
            FunctionBlocks =
            [
                new()
                {
                    Connectors =
                    [
                        new()
                        {
                            InitialValue = 1.0,
                        },
                    ],
                },
            ],
        };
        dataflow.Links.Add(new()
        {
            Source = dataflow.FunctionBlocks[0].Connectors[0],
            Destination = dataflow.FunctionBlocks[0].Connectors[0],
        });
        CommunicationConfiguration emptyCommunicationConfiguration = new();
        StartParameter startParameter = new()
        {
            Dataflow = dataflow,
            Engine = DeploymentPoolContext.StartParameter.Engine,
            DataDistribution = new()
            {
                ExternalConnections =
                [
                    new()
                    {
                        Communication = emptyCommunicationConfiguration,
                        IncomingConnectors =
                        [
                            new() { Connector = dataflow.FunctionBlocks[0].Connectors[0], },
                        ],
                        OutgoingConnectors =
                        [
                            new() { Connector = dataflow.FunctionBlocks[0].Connectors[0], },
                        ],
                    },
                ],
                RuntimeFunctionCommunications =
                [
                    emptyCommunicationConfiguration,
                ],
                SettingsPersistenceCommunications =
                [
                    new() { Communication = emptyCommunicationConfiguration, },
                ],
                VariablesPersistenceCommunications =
                [
                    new() { Communication = emptyCommunicationConfiguration, },
                ],
            },
            EngineLog = DeploymentPoolContext.StartParameter.EngineLog,
        };
        _context.Deployment.WriteStartParameter(startParameter);

        var act = FluentActions.Awaiting(() => _context.EnginePool.RecoverDeploymentAsync("1", context, CancellationToken.None));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not*create*launch configuration*");
    }
}

public sealed class RemoveDeployment
{
    [Fact]
    public async Task Can_remove_deployment_Async()
    {
        using DeploymentPoolContext context = new();
        context.Deployment.WriteStartParameter(DeploymentPoolContext.StartParameter);
        var deployment = Substitute.For<IDeployment>();
        context.EnginePool._deployments.TryAdd("1", deployment).Should().BeTrue();

        await context.EnginePool.RemoveDeploymentAsync("1");

        context.EnginePool._deployments.Should().BeEmpty();
        await deployment.Received().DisposeAsync();
        context.Deployment.FileSystem.Directory.EnumerateDirectories(context.Config.Value.DeploymentsDirectory, "*", SearchOption.AllDirectories).Select(d => new DirectoryInfo(d).Name)
            .Should().NotContain("1");
    }
}

public sealed class TryGetDeployment : IDisposable
{
    private readonly DeploymentPoolContext _context = new();
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Returns_true_with_deployment()
    {
        var deployment = Substitute.For<IDeployment>();
        _context.EnginePool._deployments.TryAdd("1", deployment).Should().BeTrue();
        _context.EnginePool._deployments.TryAdd("2", Substitute.For<IDeployment>()).Should().BeTrue();

        _context.EnginePool.TryGetDeployment("1", out var result)
            .Should().BeTrue();
        result.Should().Be(deployment);
    }

    [Fact]
    public void Returns_false_without_deployment()
    {
        _context.EnginePool._deployments.TryAdd("2", Substitute.For<IDeployment>()).Should().BeTrue();

        _context.EnginePool.TryGetDeployment("1", out var result)
            .Should().BeFalse();
        result.Should().BeNull();
    }
}

internal sealed class DeploymentPoolContext : IDisposable
{
    internal static StartParameter StartParameter { get; } = new()
    {
        Engine = new()
        {
            UniqueIdentifier = "00000000-0000-0000-0000-000000000001",
            Name = "main",
        },
        EngineLog = new()
        {
            Communication = new() { Id = "8B58C1F9-D120-42E9-8F63-B7C62FE1C415", },
        },
    };

    internal DeploymentPool EnginePool { get; }
    internal TemporaryDirectoryDeployment Deployment { get; }
    internal IOptions<HostConfig> Config { get; }

    internal DeploymentPoolContext()
    {
        Deployment = new("1", "context");
        Deployment.PlaceAssemblies();
        Config = Substitute.For<IOptions<HostConfig>>();
        Config.Value.Returns(new HostConfig { DeploymentsDirectory = Deployment.Path, });
        EnginePool = new(Config, Deployment.FileSystem);
    }

    public void Dispose() => Deployment.Dispose();
}
