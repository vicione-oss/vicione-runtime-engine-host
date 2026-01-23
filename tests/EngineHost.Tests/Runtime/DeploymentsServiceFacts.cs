using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Testably.Abstractions.Testing;
using ViciOne.ManagedEngine.PackageResolver;
using ViciOne.ManagedEngine.Pipelines;
using ViciOne.ManagedEngine.Pipelines.Requests;
using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

public class DeploymentsService_StartingAsync
{
    [Fact]
    public async Task Creates_deployments_directory()
    {
        var logger = Substitute.For<ILogger<DeploymentsService>>();
        var options = Substitute.For<IOptions<HostConfig>>();
        var deploymentPool = Substitute.For<IDeploymentPool>();
        var engineChain = Substitute.For<IEngineChain>();
        var contextPool = Substitute.For<IContextPool>();
        var mediator = Substitute.For<IMediator>();
        DirectoryMock tempDirectory = new();
        var deploymentsDirectory = tempDirectory.FileSystem.Path.Combine(tempDirectory.Path, Guid.NewGuid().ToString());
        options.Value.Returns(new HostConfig { DeploymentsDirectory = deploymentsDirectory, });

        DeploymentsService service = new(options, deploymentPool, contextPool, engineChain, mediator, logger, tempDirectory.FileSystem);

        await service.StartingAsync(CancellationToken.None);

        tempDirectory.FileSystem.Directory.Exists(deploymentsDirectory).Should().BeTrue();
    }
}

public class DeploymentsService_StartAsync
{
    [Fact]
    public async Task Recovers_deployments()
    {
        TestLogger<DeploymentsService> logger = new();
        var options = Substitute.For<IOptions<HostConfig>>();
        var deploymentPool = Substitute.For<IDeploymentPool>();
        var engineChain = Substitute.For<IEngineChain>();
        var contextPool = Substitute.For<IContextPool>();
        var mediator = Substitute.For<IMediator>();
        var id = Guid.NewGuid().ToString();
        TemporaryDirectoryDeployment deployment = new(id, ContextPool.GetUniqueHash([]));
        deployment.PlaceAssemblies();
        deployment.WriteStartParameter();
        deployment.WriteDeployParameter(new()
        {
            EngineChainIndex = 23,
        });
        options.Value.Returns(new HostConfig { DeploymentsDirectory = deployment.Path, });

        DeploymentsService service = new(options, deploymentPool, contextPool, engineChain, mediator, logger, deployment.FileSystem);

        await service.StartAsync(CancellationToken.None);

        await contextPool.Received(1).RegisterDeploymentAsync(Arg.Is(id), Arg.Any<IReadOnlyCollection<PackageReference>>(), Arg.Any<CancellationToken>());
        await deploymentPool.Received(1).RecoverDeploymentAsync(Arg.Is(id), Arg.Any<AssemblyLoadContext>(), Arg.Any<CancellationToken>());
        engineChain.Received(1).AddChainLink(Arg.Is(id), Arg.Any<Func<ulong>>(), Arg.Is(23));
        await deploymentPool.DidNotReceiveWithAnyArgs().RemoveDeploymentAsync(default!);
        await contextPool.DidNotReceiveWithAnyArgs().UnregisterDeploymentAsync(default!);
        await engineChain.DidNotReceiveWithAnyArgs().RemoveChainLinkAsync(default!);
        var logEntry = logger.Entries.Should().ContainSingle(e => e.EventId.Id == 2).Subject;
        logEntry.Exception.Should().BeNull();
        logEntry.LogLevel.Should().Be(LogLevel.Debug);
        logEntry.Message.Should().MatchEquivalentOf($"*deployment*'{id}'*recovered*at*23*");
    }

    [Fact]
    public async Task Removes_recovered_parts_if_recovery_fails()
    {
        TestLogger<DeploymentsService> logger = new(LogLevel.Error);
        var options = Substitute.For<IOptions<HostConfig>>();
        var deploymentPool = Substitute.For<IDeploymentPool>();
        var engineChain = Substitute.For<IEngineChain>();
        var contextPool = Substitute.For<IContextPool>();
        var mediator = Substitute.For<IMediator>();
        var id = Guid.NewGuid().ToString();
        TemporaryDirectoryDeployment deployment = new(id, ContextPool.GetUniqueHash([]));
        deployment.PlaceAssemblies();
        deployment.WriteStartParameter();
        options.Value.Returns(new HostConfig { DeploymentsDirectory = deployment.Path, });

        DeploymentsService service = new(options, deploymentPool, contextPool, engineChain, mediator, logger, deployment.FileSystem);

        await service.StartAsync(CancellationToken.None);

        await deploymentPool.Received(1).RemoveDeploymentAsync(Arg.Is(id));
        await contextPool.Received(1).UnregisterDeploymentAsync(Arg.Is(id));
        await engineChain.Received(1).RemoveChainLinkAsync(Arg.Is(id));
        logger.Calls.Should().Be(1);
        logger.EventId.Id.Should().Be(3);
        logger.Exception.Should().BeOfType<InvalidOperationException>();
        logger.LogLevel.Should().Be(LogLevel.Error);
        logger.Message.Should().MatchEquivalentOf($"*deployment*'{id}'*not*recovered*");
    }
}

public class DeploymentsService_StopAsync
{
    [Fact]
    public async Task Stops_deployments()
    {
        var fileSystem = new MockFileSystem();
        TestLogger<DeploymentsService> logger = new();
        var options = Substitute.For<IOptions<HostConfig>>();
        var deploymentPool = Substitute.For<IDeploymentPool>();
        var engineChain = Substitute.For<IEngineChain>();
        var contextPool = Substitute.For<IContextPool>();
        var mediator = Substitute.For<IMediator>();
        var id = Guid.NewGuid().ToString();
        deploymentPool.GetStartedDeployments().Returns([id,]);

        DeploymentsService service = new(options, deploymentPool, contextPool, engineChain, mediator, logger, fileSystem);

        await service.StopAsync(CancellationToken.None);

        await engineChain.Received(1).StopAsync();
        await mediator.Received(1).Send<StopEngine, bool>(Arg.Is<StopEngine>(r => r.DeploymentIdentifier == id), Arg.Any<CancellationToken>());
        var logEntry = logger.Entries.Should().ContainSingle(e => e.EventId.Id == 21).Subject;
        logEntry.Exception.Should().BeNull();
        logEntry.LogLevel.Should().Be(LogLevel.Debug);
        logEntry.Message.Should().MatchEquivalentOf($"*deployment*'{id}'*stopped*");
    }

    [Fact]
    public async Task Logs_stop_failures()
    {
        var fileSystem = new MockFileSystem();
        TestLogger<DeploymentsService> logger = new(LogLevel.Error);
        var options = Substitute.For<IOptions<HostConfig>>();
        var deploymentPool = Substitute.For<IDeploymentPool>();
        var engineChain = Substitute.For<IEngineChain>();
        var contextPool = Substitute.For<IContextPool>();
        var mediator = Substitute.For<IMediator>();
        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();
        deploymentPool.GetStartedDeployments().Returns([id1, id2]);
        mediator.Send<StopEngine, bool>(Arg.Is<StopEngine>(r => r.DeploymentIdentifier == id1), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Pipeline failed."));

        DeploymentsService service = new(options, deploymentPool, contextPool, engineChain, mediator, logger, fileSystem);

        await service.StopAsync(CancellationToken.None);

        await engineChain.Received(1).StopAsync();
        await mediator.Received(1).Send<StopEngine, bool>(Arg.Is<StopEngine>(r => r.DeploymentIdentifier == id2), Arg.Any<CancellationToken>());
        logger.Calls.Should().Be(1);
        logger.EventId.Id.Should().Be(22);
        logger.Exception.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("Pipeline failed.");
        logger.LogLevel.Should().Be(LogLevel.Error);
        logger.Message.Should().MatchEquivalentOf($"*deployment*'{id1}'*not*stop*");
    }
}
