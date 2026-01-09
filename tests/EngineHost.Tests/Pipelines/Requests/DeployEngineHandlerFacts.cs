using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.ManagedEngine.EngineDeployment;
using ViciOne.ManagedEngine.PackageResolver;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

public sealed class DeployEngineHandler_ : IDisposable
{
    private readonly TemporaryDirectory _temporaryDirectory;
    private readonly IDeploymentPool _deploymentPool;
    private readonly IContextPool _contextPool;
    private readonly IEngineChain _engineChain;
    private readonly TransactionContext _transactionContext = new();
    private readonly DeployEngineHandler _handler;

    public DeployEngineHandler_()
    {
        _temporaryDirectory = new TemporaryDirectory();
        _deploymentPool = Substitute.For<IDeploymentPool>();
        _contextPool = Substitute.For<IContextPool>();
        _engineChain = Substitute.For<IEngineChain>();
        _handler = new(_deploymentPool, _contextPool, _engineChain, _transactionContext);
    }

    [Fact]
    public async Task Deploys_engine_Async()
    {
        var context = ContextPool.GetUniqueHash([]);
        var deployment = await _handler.Handle(new(), CancellationToken.None);

        await _contextPool.Received().RegisterDeploymentAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<PackageReference>>(), Arg.Any<CancellationToken>());
        await _deploymentPool.Received().CreateDeploymentAsync(Arg.Any<string>(), Arg.Any<AssemblyLoadContext>(), Arg.Any<string>(), Arg.Any<DeployParameter>(), Arg.Any<CancellationToken>());
        _engineChain.Received().AddChainLink(Arg.Any<string>(), Arg.Any<Func<ulong>>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Can_handle_deployment_pool_failure_Async()
    {
        _deploymentPool.When(d => d.CreateDeploymentAsync(Arg.Any<string>(), Arg.Any<AssemblyLoadContext>(), Arg.Any<string>(), Arg.Any<DeployParameter>(), Arg.Any<CancellationToken>())).Throw<InvalidOperationException>();

        var act = _handler.Awaiting(_ => _.Handle(new(), CancellationToken.None));

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _contextPool.Received().UnregisterDeploymentAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Can_handle_engine_chain_failure_Async()
    {
        _engineChain.When(c => c.SetUpCycleTimeAsync(Arg.Any<TimeSpan>())).Throw<InvalidOperationException>();

        var act = _handler.Awaiting(_ => _.Handle(new(), CancellationToken.None));

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _deploymentPool.Received().RemoveDeploymentAsync(Arg.Any<string>());
        await _contextPool.Received().UnregisterDeploymentAsync(Arg.Any<string>());
    }

    public void Dispose()
    {
        _transactionContext.Dispose();
        _temporaryDirectory.Dispose();
    }
}
