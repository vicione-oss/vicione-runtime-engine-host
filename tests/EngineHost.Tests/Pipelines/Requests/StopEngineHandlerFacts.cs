using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NSubstitute;
using ViciOne.ManagedEngine.EngineDeployment;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

public sealed class StopEngineHandler_ : IDisposable
{
    private readonly TemporaryDirectoryDeployment _temporaryDirectoryDeployment;
    private readonly IOptions<HostConfig> _config;
    private readonly IDeploymentPool _deploymentPool;
    private readonly IEngineChain _engineChain;
    private readonly TransactionContext _transactionContext = new();
    private readonly StopEngineHandler _handler;

    public StopEngineHandler_()
    {
        _temporaryDirectoryDeployment = new TemporaryDirectoryDeployment("test", string.Empty);
        _config = Substitute.For<IOptions<HostConfig>>();
        _config.Value.Returns(new HostConfig { DeploymentsDirectory = _temporaryDirectoryDeployment.Path, });
        _deploymentPool = Substitute.For<IDeploymentPool>();
        _engineChain = Substitute.For<IEngineChain>();
        _handler = new(_deploymentPool, _engineChain, _transactionContext);
    }

    [Fact]
    public async Task Stops_deployment_Async()
    {
        _deploymentPool.DeploymentIsStarted("test").Returns(true);
        _deploymentPool.TryGetDeployment(Arg.Any<string>(), out Arg.Any<IDeployment?>()).Returns(c =>
        {
            c[1] = Substitute.For<IDeployment>();
            return true;
        });

        await _handler.Handle(new() { DeploymentIdentifier = "test", }, CancellationToken.None);

        _deploymentPool.Received().DeploymentStopped("test");
    }

    [Fact]
    public async Task Can_handle_already_stopped_deployment_Async()
    {
        _deploymentPool.DeploymentIsStarted("test").Returns(false);

        await _handler.Handle(new() { DeploymentIdentifier = "test", }, CancellationToken.None);

        _deploymentPool.DidNotReceive().DeploymentStopped(Arg.Any<string>());
    }

    public void Dispose()
    {
        _transactionContext.Dispose();
        _temporaryDirectoryDeployment.Dispose();
    }
}
