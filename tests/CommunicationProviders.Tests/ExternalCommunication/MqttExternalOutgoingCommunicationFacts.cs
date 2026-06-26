using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Mime;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using NSubstitute;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.ExternalCommunication;

public sealed class MqttExternalOutgoingCommunication_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
    {
        await using ConstructorProviderFactory providerFactory = new();

        var instance = providerFactory.CreateExternalOutgoing(
            new MqttCommunication { Host = "localhost", },
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<MqttExternalOutgoingCommunication>();
    }
}

public sealed class MqttExternalOutgoingCommunication_Connect
{
    [Fact]
    public async Task Connects_client_Async()
    {
        using MqttExternalOutgoingCommunicationContext context = new();

        await context.Communication.ConnectAsync(CancellationToken.None);

        await context.MqttClient.Received(1).Connect();
    }
}

public sealed class MqttExternalOutgoingCommunication_Disconnect
{
    [Fact]
    public async Task Disconnects_client_Async()
    {
        using MqttExternalOutgoingCommunicationContext context = new();

        await context.Communication.DisconnectAsync(CancellationToken.None);

        await context.MqttClient.Received(1).Disconnect();
    }
}

public sealed class MqttExternalOutgoingCommunication_Send : IDisposable
{
    private readonly MqttExternalOutgoingCommunicationContext _context = new();
    public void Dispose() => _context.Dispose();

    [Theory]
    [InlineData(78, "78")]
    [InlineData(123.45, "123.45")]
    [InlineData("Hello", "\"Hello\"")]
    public async Task Publishes_value_Async(object value, string payload)
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        await _context.Communication.SendAsync(8,
            [
                new()
                {
                    Channel = "A",
                    Value = value,
                },
            ], CancellationToken.None);

        messages.Should().ContainSingle().Which.ConvertPayloadToString().Should().Be(payload);
    }

    [Fact]
    public async Task Uses_channel_as_topic_Async()
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        await _context.Communication.SendAsync(8,
            [
                new()
                {
                    Channel = "A",
                },
            ], CancellationToken.None);

        var message = messages.Should().ContainSingle().Subject;
        message.Topic.Should().Be("A");
        message.TopicAlias.Should().Be(0);
        message.ResponseTopic.Should().BeNull();
    }

    [Fact]
    public async Task Uses_secure_QualityOfService_level_Async()
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        await _context.Communication.SendAsync(8,
            [
                new()
                {
                    Channel = "A",
                },
            ], CancellationToken.None);

        var message = messages.Should().ContainSingle().Subject;
        message.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
        message.Retain.Should().BeTrue();
    }

    [Fact]
    public async Task Publishes_as_json_Async()
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        await _context.Communication.SendAsync(8,
            [
                new()
                {
                    Channel = "A",
                    Value = true,
                },
            ], CancellationToken.None);

        var message = messages.Should().ContainSingle().Subject;
        message.PayloadFormatIndicator.Should().Be(MqttPayloadFormatIndicator.CharacterData);
        message.ContentType.Should().Be(MediaTypeNames.Application.Json);
    }

    [Fact]
    public async Task Publishes_meta_data_as_user_properties_Async()
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        await _context.Communication.SendAsync(8,
            [
                new()
                {
                    Timestamp = new DateTime(2020, 04, 15, 17, 18, 20, DateTimeKind.Utc),
                    Channel = "A",
                    Validity = 1,
                },
            ], CancellationToken.None);

        var message = messages.Should().ContainSingle().Subject;
        message.UserProperties.Should().BeEquivalentTo(new MqttUserProperty[]
        {
            new(MqttUserProperties.Timestamp, Encoding.UTF8.GetBytes("2020-04-15T17:18:20.0000000Z")),
            new(MqttUserProperties.Validity, Encoding.UTF8.GetBytes("1")),
            new(MqttUserProperties.EngineCycle, Encoding.UTF8.GetBytes("8")),
            new(MqttUserProperties.Type, Encoding.UTF8.GetBytes(typeof(object).AssemblyQualifiedName ?? string.Empty)),
        });
    }

    [Fact]
    public async Task Can_send_null_value_Async()
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        await _context.Communication.SendAsync(8,
            [
                new()
                {
                    Channel = "A",
                    Value = null,
                },
            ], CancellationToken.None);

        var message = messages.Should().ContainSingle().Subject;
        message.ConvertPayloadToString().Should().Be("null");
    }

    [Fact]
    public async Task Can_send_complex_value_Async()
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        await _context.Communication.SendAsync(8,
            [
                new()
                {
                    Channel = "A",
                    Value = new CancelEventArgs(true),
                },
            ], CancellationToken.None);

        var message = messages.Should().ContainSingle().Subject;
        JsonSerializer.Deserialize<CancelEventArgs>(message.ConvertPayloadToString(), JsonSetup.CreatePreserveTypeOptions()).Should().BeEquivalentTo(
            new CancelEventArgs(true));
    }

    [Fact]
    public async Task Logs_error_for_not_publishable_value_Async()
    {
        _context.NameResolver.ResolveName(_context.Connection).Returns(_context.Connection);
        InvalidOperationException exception = new();
        _context.MqttClient.WhenForAnyArgs(_ => _.Publish(default!)).Throw(exception);

        await _context.Communication.SendAsync(0, [new() { Channel = "abc123", },], CancellationToken.None);

        _context.Logger.Calls.Should().Be(1);
        _context.Logger.LogLevel.Should().Be(LogLevel.Error);
        _context.Logger.EventId.Should().Be(new EventId(12, nameof(MqttExternalCommunicationLog.MessageNotSend)));
        _context.Logger.Exception.Should().BeSameAs(exception);
        _context.Logger.Message.Should().MatchEquivalentOf($"*not*send*message*to*{_context.Connection}*({_context.Connection})*at*abc123*");
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(null, true)]
    public async Task Can_configure_retain_flag_Async(bool? retain, bool expectedRetain)
    {
        List<MqttApplicationMessage> messages = [];
        using MqttExternalOutgoingCommunicationContext context = new(communication => communication.Retain = retain);
        await context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        await context.Communication.SendAsync(3,
            [
                new()
                {
                    Channel = "A",
                    Value = true,
                },
            ], CancellationToken.None);

        var message = messages.Should().ContainSingle().Subject;
        message.Retain.Should().Be(expectedRetain);
    }

    [Theory]
    [InlineData(10u, 10u)]
    [InlineData(0u, 0u)]
    [InlineData(null, 0u)]
    public async Task Can_configure_message_expiry_interval_flag_Async(uint? expiryInterval, uint expectedExpiryInterval)
    {
        List<MqttApplicationMessage> messages = [];
        using MqttExternalOutgoingCommunicationContext context = new(communication => communication.MessageExpiryInterval = expiryInterval);
        await context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        await context.Communication.SendAsync(3,
            [
                new()
                {
                    Channel = "A",
                    Value = true,
                },
            ], CancellationToken.None);

        var message = messages.Should().ContainSingle().Subject;
        message.MessageExpiryInterval.Should().Be(expectedExpiryInterval);
    }
}

public sealed class MqttExternalOutgoingCommunication_Dispose
{
    [Fact]
    public void Disposes_client()
    {
        using MqttExternalOutgoingCommunicationContext context = new();

        context.Communication.Dispose();

        ((IDisposable)context.MqttClient).Received(1).Dispose();
    }
}

internal sealed class MqttExternalOutgoingCommunicationContext : IDisposable
{
    internal string Connection { get; } = "PublicExchange";
    internal string Engine { get; } = "main";

    internal IVirtualMqttClient MqttClient { get; }
    internal TestLogger<MqttExternalOutgoingCommunication> Logger { get; }
    internal INameResolver NameResolver { get; }
    internal MqttExternalOutgoingCommunication Communication { get; }

    internal MqttExternalOutgoingCommunicationContext(Action<MqttCommunication>? configureCommunication = null)
    {
        MqttClient = Substitute.For<IVirtualMqttClient, IDisposable>();
        Logger = new();
        ExternalOutgoingCommunicationOptions options = new()
        {
            ConnectionUniqueIdentifier = Connection,
            EngineUniqueIdentifier = Engine,
        };
        NameResolver = Substitute.For<INameResolver>();

        MqttCommunication communication = new()
        {
            QualityOfService = 2,
        };
        configureCommunication?.Invoke(communication);

        Communication = new(communication, options, MqttClient, Logger, NameResolver, AssemblyLoadContext.Default);
    }

    public void Dispose()
    {
        Communication.Dispose();
        MqttClient.Dispose();
    }
}
