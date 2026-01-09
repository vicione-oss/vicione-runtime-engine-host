using System;
using System.Runtime.Loader;
using System.Text.Json;
using AwesomeAssertions;
using ViciOne.ManagedEngine.Communication;
using Xunit;

namespace ViciOne.ManagedEngine.EngineDeployment;

public class Deployment_CreateAsync
{
    [Fact]
    public void Recognizes_wrong_configuration()
    {
        var startParameter = JsonSerializer.Serialize(new StartParameter(), JsonSetup.CreatePreserveTypeOptions());

        var act = FluentActions.Awaiting(() => Deployment.Create(startParameter, AssemblyLoadContext.Default));

        act.Should().ThrowAsync<ArgumentException>().WithMessage("*not*create*launch*configuration*");
    }
}
