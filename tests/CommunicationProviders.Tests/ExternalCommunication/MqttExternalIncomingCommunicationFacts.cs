using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Extensions;
using NSubstitute;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.ExternalCommunication;

public sealed class MqttExternalIncomingCommunication_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
    {
        await using ConstructorProviderFactory providerFactory = new();

        var instance = providerFactory.CreateExternalIncoming(
            new MqttCommunication { Host = "localhost", },
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<MqttExternalIncomingCommunication>();
    }
}

public sealed class MqttExternalIncomingCommunication_Constructor
{
    [Fact]
    public void Starts_message_processing()
    {
        using MqttExternalIncomingCommunicationContext context = new();

        context.MqttClient.Received(1).MessageReceived += Arg.Any<Func<MqttApplicationMessageReceivedEventArgs, Task>>();
    }
}

public sealed class MqttExternalIncomingCommunication_Connect : IDisposable
{
    private readonly MqttExternalIncomingCommunicationContext _context = new();
    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Connects_client_Async()
    {
        await _context.Communication.ConnectAsync(CancellationToken.None);

        await _context.MqttClient.Received(1).Connect();
    }

    [Fact]
    public async Task Subscribes_channels_Async()
    {
        await _context.Communication.ConnectAsync(CancellationToken.None);

        await _context.MqttClient.Received(1).Subscribe("topic", 0, true);
    }
}

public sealed class MqttExternalIncomingCommunication_Disconnect : IDisposable
{
    private readonly MqttExternalIncomingCommunicationContext _context = new();
    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Disconnects_client_Async()
    {
        await _context.Communication.DisconnectAsync(CancellationToken.None);

        await _context.MqttClient.Received(1).Disconnect();
    }

    [Fact]
    public async Task Unsubscribes_channels_Async()
    {
        await _context.Communication.DisconnectAsync(CancellationToken.None);

        await _context.MqttClient.Received(1).Unsubscribe("topic");
    }
}

public sealed class MqttExternalIncomingCommunication_Received : IDisposable
{
    private readonly MqttExternalIncomingCommunicationContext _context = new();
    public void Dispose() => _context.Dispose();

    private readonly JsonSerializerOptions _serializerOptions = JsonSetup.CreatePreserveTypeOptions();

    [Theory]
    [InlineData("78", 78)]
    [InlineData("123.45", 123.45)]
    [InlineData("\"Hello\"", "Hello")]
    public void Receives_values(string payload, object value)
    {
        var values = new List<ExternalValue>();
        _context.Communication.Received += values.AddRange;

        var builder = new MqttApplicationMessageBuilder()
            .WithTopic("A")
            .WithPayload(payload)
            .WithUserProperty(MqttUserProperties.Timestamp, Encoding.UTF8.GetBytes("2020-04-16T10:14:40.0000000+0200"))
            .WithUserProperty(MqttUserProperties.Validity, Encoding.UTF8.GetBytes("1"))
            .WithUserProperty(MqttUserProperties.Type, Encoding.UTF8.GetBytes(value.GetType().AssemblyQualifiedName ?? string.Empty));
        var e = new MqttApplicationMessageReceivedEventArgs("B", builder.Build(), new(), (_, _) => Task.CompletedTask);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(e);

        e.ProcessingFailed.Should().BeFalse();
        values.Should().BeEquivalentTo(new ExternalValue[]
        {
            new()
            {
                Timestamp = new DateTime(2020, 04, 16, 8, 14, 40, DateTimeKind.Utc),
                Channel = "A",
                Validity = 1,
                Value = value,
            },
        });
    }

    [Fact]
    public void Can_receive_null_value()
    {
        var values = new List<ExternalValue>();
        _context.Communication.Received += values.AddRange;

        var builder = new MqttApplicationMessageBuilder()
            .WithTopic("A")
            .WithJsonPayload(JsonSerializer.Serialize<object?>(null, _serializerOptions))
            .WithUserProperty(MqttUserProperties.Timestamp, Encoding.UTF8.GetBytes("2020-04-16T08:14:40.0000000Z"))
            .WithUserProperty(MqttUserProperties.Validity, Encoding.UTF8.GetBytes("0"))
            .WithUserProperty(MqttUserProperties.Type, Encoding.UTF8.GetBytes(typeof(object).AssemblyQualifiedName ?? string.Empty));
        var e = new MqttApplicationMessageReceivedEventArgs("B", builder.Build(), new(), (_, _) => Task.CompletedTask);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(e);

        values.Should().BeEquivalentTo(new ExternalValue[]
        {
            new()
            {
                Timestamp = new DateTime(2020, 04, 16, 8, 14, 40, DateTimeKind.Utc),
                Channel = "A",
                Validity = 0,
                Value = null,
            },
        });
    }

    [Fact]
    public void Can_receive_complex_value()
    {
        var values = new List<ExternalValue>();
        _context.Communication.Received += values.AddRange;
        var builder = new MqttApplicationMessageBuilder()
            .WithTopic("A")
            .WithJsonPayload(JsonSerializer.Serialize(new CancelEventArgs(true), _serializerOptions))
            .WithUserProperty(MqttUserProperties.Timestamp, Encoding.UTF8.GetBytes("2020-04-16T09:14:40.0000000+0100"))
            .WithUserProperty(MqttUserProperties.Validity, Encoding.UTF8.GetBytes("0"))
            .WithUserProperty(MqttUserProperties.Type, Encoding.UTF8.GetBytes(typeof(CancelEventArgs).AssemblyQualifiedName ?? string.Empty));
        var e = new MqttApplicationMessageReceivedEventArgs("B", builder.Build(), new(), (_, _) => Task.CompletedTask);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(e);

        values.Should().BeEquivalentTo(new ExternalValue[]
        {
            new()
            {
                Timestamp = new DateTime(2020, 04, 16, 8, 14, 40, DateTimeKind.Utc),
                Channel = "A",
                Validity = 0,
                Value = new CancelEventArgs(true),
            },
        });
    }

    [Fact]
    public void Logs_error_for_unparsable_payload()
    {
        _context.NameResolver.ResolveName(_context.Connection).Returns(_context.Connection);
        var values = new List<ExternalValue>();
        _context.Communication.Received += values.AddRange;

        var builder = new MqttApplicationMessageBuilder()
            .WithTopic("A")
            .WithPayload("invalid payload text")
            .WithUserProperty(MqttUserProperties.Timestamp, Encoding.UTF8.GetBytes("2020-04-16T08:14:40.0000000Z"))
            .WithUserProperty(MqttUserProperties.Validity, Encoding.UTF8.GetBytes("1"))
            .WithUserProperty(MqttUserProperties.Type, Encoding.UTF8.GetBytes(typeof(string).AssemblyQualifiedName ?? string.Empty));
        var e = new MqttApplicationMessageReceivedEventArgs("X", builder.Build(), new(), (_, _) => Task.CompletedTask);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(e);

        e.ProcessingFailed.Should().BeFalse();
        values.Should().BeEmpty();
        _context.Logger.Calls.Should().Be(2);
        _context.Logger.LogLevel.Should().Be(LogLevel.Error);
        _context.Logger.EventId.Should().Be(new EventId(10, nameof(MqttExternalCommunicationLog.MessageNotProcessable)));
        _context.Logger.Exception.Should().BeAssignableTo<JsonException>();
        _context.Logger.Message.Should().MatchEquivalentOf($"*not*process*message*from*{_context.Connection}*({_context.Connection})*of*X*at*A*");
    }

    [Fact]
    public void Logs_error_when_user_properties_are_null()
    {
        _context.NameResolver.ResolveName(_context.Connection).Returns(_context.Connection);
        var values = new List<ExternalValue>();
        _context.Communication.Received += values.AddRange;

        var builder = new MqttApplicationMessageBuilder()
            .WithTopic("A")
            .WithPayload("invalid payload text");
        var e = new MqttApplicationMessageReceivedEventArgs("X", builder.Build(), new(), (_, _) => Task.CompletedTask);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(e);

        e.ProcessingFailed.Should().BeFalse();
        values.Should().BeEmpty();
        _context.Logger.Calls.Should().Be(2);
        _context.Logger.LogLevel.Should().Be(LogLevel.Error);
        _context.Logger.EventId.Should().Be(new EventId(10, nameof(MqttExternalCommunicationLog.MessageNotProcessable)));
        _context.Logger.Exception.Should().BeAssignableTo<ArgumentNullException>().Which.Message.Should().MatchEquivalentOf("*null*UserProperties*");
        _context.Logger.Message.Should().MatchEquivalentOf($"*not*process*message*from*{_context.Connection}*({_context.Connection})*of*X*at*A*");
    }

    [Fact]
    public void Logs_error_for_missing_user_property()
    {
        _context.NameResolver.ResolveName(_context.Connection).Returns(_context.Connection);
        var values = new List<ExternalValue>();
        _context.Communication.Received += values.AddRange;

        var builder = new MqttApplicationMessageBuilder()
            .WithTopic("A")
            .WithPayload("78")
            .WithUserProperty(MqttUserProperties.Timestamp, Encoding.UTF8.GetBytes("2020-04-16T10:14:40.0000000+0200"))
            .WithUserProperty(MqttUserProperties.Type, Encoding.UTF8.GetBytes(typeof(int).AssemblyQualifiedName ?? string.Empty));
        var e = new MqttApplicationMessageReceivedEventArgs("X", builder.Build(), new(), (_, _) => Task.CompletedTask);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(e);

        e.ProcessingFailed.Should().BeFalse();
        values.Should().BeEmpty();
        _context.Logger.Calls.Should().Be(2);
        _context.Logger.LogLevel.Should().Be(LogLevel.Error);
        _context.Logger.EventId.Should().Be(new EventId(10, nameof(MqttExternalCommunicationLog.MessageNotProcessable)));
        _context.Logger.Exception.Should().BeAssignableTo<InvalidOperationException>().Which.Message.Should().MatchEquivalentOf("*property*Validity*not*found*");
        _context.Logger.Message.Should().MatchEquivalentOf($"*not*process*message*from*{_context.Connection}*({_context.Connection})*of*X*at*A*");
    }
}

public sealed class MqttExternalIncomingCommunication_Dispose : IDisposable
{
    private readonly MqttExternalIncomingCommunicationContext _context = new();
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Disposes_client()
    {
        _context.Communication.Dispose();

        ((IDisposable)_context.MqttClient).Received(1).Dispose();
    }

    [Fact]
    public void Stops_message_processing()
    {
        _context.Communication.Dispose();
        _context.MqttClient.Received(1).MessageReceived -= Arg.Any<Func<MqttApplicationMessageReceivedEventArgs, Task>>();
    }
}

internal sealed class MqttExternalIncomingCommunicationContext : IDisposable
{
    internal string Connection { get; } = "PublicExchange";
    internal string Engine { get; } = "main";

    internal IVirtualMqttClient MqttClient { get; }
    internal TestLogger<MqttExternalIncomingCommunication> Logger { get; }
    internal INameResolver NameResolver { get; }
    internal MqttExternalIncomingCommunication Communication { get; }

    internal MqttExternalIncomingCommunicationContext()
    {
        MqttClient = Substitute.For<IVirtualMqttClient, IDisposable>();
        Logger = new();
        ExternalIncomingCommunicationOptions options = new()
        {
            Channels = ["topic"],
            ConnectionUniqueIdentifier = Connection,
            EngineUniqueIdentifier = Engine,
        };
        NameResolver = Substitute.For<INameResolver>();
        Communication = new(new(), Logger, options, MqttClient, NameResolver, AssemblyLoadContext.Default);
    }

    public void Dispose()
    {
        Communication.Dispose();
        MqttClient.Dispose();
    }
}
