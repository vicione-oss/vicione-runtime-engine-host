using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.ManagedEngine.EngineDeployment;
using ViciOne.ManagedEngine.PackageResolver;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

public sealed class GetPackagesHandler_Handle : IDisposable
{
    private readonly IDeploymentPool _deploymentPool = Substitute.For<IDeploymentPool>();
    private readonly TransactionContext _transactionContext = new();
    private readonly GetPackagesHandler _handler;

    public GetPackagesHandler_Handle()
        => _handler = new(_deploymentPool, _transactionContext);

    public void Dispose()
        => _transactionContext.Dispose();

    [Fact]
    public async Task Returns_expected_state_Async()
    {
        var deployment = Substitute.For<IDeployment>();

        _deploymentPool.GetDeployParameter(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DeployParameter() { PackageReferences = [new PackageReference() { Name = "Test", Version = "0.0.1" }] });

        _deploymentPool.TryGetDeployment("main", out Arg.Any<IDeployment?>()).Returns(c =>
        {
            c[1] = deployment;
            return true;
        });

        var state = await _handler.Handle(new() { DeploymentIdentifier = "main", }, CancellationToken.None);

        state.Should().ContainSingle().Which.Name.Should().Be("Test");
        state.Should().ContainSingle().Which.Version.Should().Be("0.0.1");
    }

    [Fact]
    public async Task Throws_if_deployment_does_not_exist_Async()
    {
        _deploymentPool.TryGetDeployment("main", out Arg.Any<IDeployment?>()).Returns(false);

        var act = () => _handler.Handle(new() { DeploymentIdentifier = "main", }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not*find*deployment*main*");
    }
}
