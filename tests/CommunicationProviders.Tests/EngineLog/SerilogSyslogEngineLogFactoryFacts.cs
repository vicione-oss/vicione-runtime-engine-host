using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.EngineLog;

public sealed class SerilogSyslogEngineLogFactory_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
    {
        await using ConstructorProviderFactory providerFactory = new();

        var instance = providerFactory.CreateEngineLogFactory(new SerilogSyslogCommunication(), string.Empty);

        instance.Should().NotBeNull().And.BeOfType<SerilogSyslogEngineLogFactory>();
    }
}

public class SerilogSyslogEngineLogFactory_AddLogging
{
    [Fact]
    public void Adds_logger_provider()
    {
        SerilogSyslogEngineLogFactory factory = new(new());
        ServiceCollection services = [];

        factory.AddLogging(services, LogLevel.None);

        var test = services.Should().Contain(sd => sd.ServiceType == typeof(ILoggerProvider)).Subject;
        test.ImplementationFactory.Should().NotBeNull();
        test.ImplementationFactory!.Method.ReturnType.Should().Be<SerilogLoggerProvider>();
    }
}
