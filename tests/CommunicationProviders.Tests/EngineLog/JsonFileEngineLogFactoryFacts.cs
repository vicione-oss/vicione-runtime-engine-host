using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.EngineLog;

public class JsonFileEngineLogFactory_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
    {
        await using ConstructorProviderFactory providerFactory = new();

        var instance = providerFactory.CreateEngineLogFactory(new FileSystemCommunication(), string.Empty);

        instance.Should().NotBeNull().And.BeOfType<JsonFileEngineLogFactory>();
    }
}

public class JsonFileEngineLogFactory_AddLogging
{
    [Fact]
    public void Adds_logger_provider()
    {
        JsonFileEngineLogFactory factory = new(new(), string.Empty);
        ServiceCollection services = [];

        factory.AddLogging(services, LogLevel.None);

        services.Should().Contain(sd => sd.ServiceType == typeof(ILoggerProvider)).Which.ImplementationInstance.Should().BeOfType<JsonFileLoggerProvider>();
    }
}
