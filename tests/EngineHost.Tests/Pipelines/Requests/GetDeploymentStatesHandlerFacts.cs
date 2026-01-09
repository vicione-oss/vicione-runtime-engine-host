using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.ManagedEngine.EngineDeployment;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

public sealed class GetDeploymentStatesHandler_ : IDisposable
{
    private readonly IDeploymentPool _deploymentPool = Substitute.For<IDeploymentPool>();
    private readonly TransactionContext _transactionContext = new();
    private readonly GetDeploymentStatesHandler _handler;

    public GetDeploymentStatesHandler_()
        => _handler = new(_deploymentPool, _transactionContext);

    public void Dispose() => _transactionContext.Dispose();

    [Fact]
    public async Task Returns_expected_state_dictionary()
    {
        var deployment = Substitute.For<IDeployment>();
        deployment.GetStateAsync(Arg.Any<CancellationToken>()).Returns(EngineState.Running);
        _deploymentPool.TryGetDeployment("main", out Arg.Any<IDeployment?>()).Returns(c =>
        {
            c[1] = deployment;
            return true;
        });
        _deploymentPool.GetDeployments().Returns(c => ["main"]);

        var states = await _handler.Handle(new(), CancellationToken.None);

        var which = states.Should().ContainSingle().Which;
        which.Value.Should().Be(EngineState.Running);
        which.Key.Should().Be("main");
    }

    [Fact]
    public async Task Returns_nothing_when_deployment_is_not_accessible()
    {
        _deploymentPool.GetDeployments().Returns(c => ["main"]);

        var states = await _handler.Handle(new(), CancellationToken.None);

        states.Should().BeEmpty();
    }
}
