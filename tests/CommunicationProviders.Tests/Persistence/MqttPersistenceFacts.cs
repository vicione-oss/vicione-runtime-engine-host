using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Mime;
using System.Runtime.Loader;
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

namespace ViciOne.ManagedEngine.Persistence;

public sealed class MqttPersistence_General
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_of_settings_persistence_Async()
    {
        await using ConstructorProviderFactory providerFactory = new();

        var instance = providerFactory.CreateSettingsPersistence(
            new MqttCommunication { Host = "localhost", },
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<MqttPersistence>();
    }

    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_of_variables_persistence_Async()
    {
        await using ConstructorProviderFactory providerFactory = new();

        var instance = providerFactory.CreateVariablesPersistence(
            new MqttCommunication { Host = "localhost", },
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<MqttPersistence>();
    }
}

public sealed class MqttPersistence_Connect
{
    [Fact]
    public async Task Connects_client_Async()
    {
        using MqttPersistenceContext context = new();

        await context.Persistence.ConnectAsync(CancellationToken.None);

        await context.MqttClient.Received(1).Connect();
    }
}

public sealed class MqttPersistence_Disconnect
{
    [Fact]
    public async Task Connects_client_Async()
    {
        using MqttPersistenceContext context = new();

        await context.Persistence.DisconnectAsync(CancellationToken.None);

        await context.MqttClient.Received(1).Disconnect();
    }
}

public sealed class MqttPersistence_Load
{
    [Fact]
    public async Task Returns_empty_list_Async()
    {
        using MqttPersistenceContext context = new();

        var actual = await context.Persistence.LoadAsync(CancellationToken.None);

        actual.Should().BeEmpty();
    }
}

public sealed class MqttPersistence_Save : IDisposable
{
    private readonly MqttPersistenceContext _context = new();
    public void Dispose() => _context.Dispose();

    [Theory]
    [InlineData(78, "78")]
    [InlineData(123.45, "123.45")]
    [InlineData("Hello", "\"Hello\"")]
    public async Task Publishes_value_Async(object value, string payload)
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        await _context.Persistence.SaveAsync(
        [
            new()
            {
                UniqueIdentifier = "a",
                Time = new DateTime(2020, 9, 28, 12, 5, 14, DateTimeKind.Utc),
                Value = value,
            },
        ], CancellationToken.None);

        var message = messages.Should().ContainSingle().Subject;
        message.Topic.Should().Be("a");
        message.TopicAlias.Should().Be(0);
        message.ResponseTopic.Should().BeNull();
        message.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
        message.Retain.Should().BeTrue();
        message.PayloadFormatIndicator.Should().Be(MqttPayloadFormatIndicator.CharacterData);
        message.ContentType.Should().Be(MediaTypeNames.Application.Json);
        message.UserProperties.Should().BeEquivalentTo(new MqttUserProperty[]
        {
            new(MqttUserProperties.Timestamp, "2020-09-28T12:05:14.0000000Z"),
            new(MqttUserProperties.Type, value.GetType().AssemblyQualifiedName),
        });
        message.ConvertPayloadToString().Should().Be(payload);
    }

    [Fact]
    public async Task Can_send_null_value_Async()
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        PersistenceEntry persistenceEntry = new()
        {
            UniqueIdentifier = "a",
            Value = null,
        };
        await _context.Persistence.SaveAsync([persistenceEntry,], CancellationToken.None);

        var message = messages.Should().ContainSingle().Subject;
        message.ConvertPayloadToString().Should().Be("null");
    }

    [Fact]
    public async Task Can_send_complex_value_Async()
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        PersistenceEntry persistenceEntry = new()
        {
            UniqueIdentifier = "a",
            Value = new CancelEventArgs(true),
        };
        await _context.Persistence.SaveAsync([persistenceEntry,], CancellationToken.None);

        var message = messages.Should().ContainSingle().Subject;
        JsonSerializer.Deserialize<CancelEventArgs>(message.ConvertPayloadToString(), JsonSetup.CreatePreserveTypeOptions()).Should().BeEquivalentTo(
            new CancelEventArgs(true));
    }

    [Fact]
    public async Task Logs_error_if_persistence_entry_could_not_be_published_Async()
    {
        _context.NameResolver.ResolveName("a").Returns("ValueCount");
        InvalidOperationException exception = new();
        _context.MqttClient.WhenForAnyArgs(_ => _.Publish(default!)).Throw(exception);

        await _context.Persistence.SaveAsync([new() { UniqueIdentifier = "a", },], CancellationToken.None);

        _context.Logger.Calls.Should().Be(1);
        _context.Logger.LogLevel.Should().Be(LogLevel.Error);
        _context.Logger.EventId.Should().Be(new EventId(2, nameof(MqttPersistenceLog.PersistenceEntryNotSend)));
        _context.Logger.Exception.Should().BeSameAs(exception);
        _context.Logger.Message.Should().MatchEquivalentOf($"*not*send*persistence entry*'a'*(ValueCount)*at*'a'*");
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(null, true)]
    public async Task Can_configure_retain_flag_Async(bool? retain, bool expectedRetain)
    {
        List<MqttApplicationMessage> messages = [];
        using MqttPersistenceContext context = new(communication => communication.Retain = retain);
        await context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        PersistenceEntry persistenceEntry = new()
        {
            UniqueIdentifier = "a",
            Value = null,
        };
        await context.Persistence.SaveAsync([persistenceEntry], CancellationToken.None);

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
        using MqttPersistenceContext context = new(communication => communication.MessageExpiryInterval = expiryInterval);
        await context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        PersistenceEntry persistenceEntry = new()
        {
            UniqueIdentifier = "a",
            Value = null,
        };
        await context.Persistence.SaveAsync([persistenceEntry,], CancellationToken.None);

        var message = messages.Should().ContainSingle().Subject;
        message.MessageExpiryInterval.Should().Be(expectedExpiryInterval);
    }
}

public sealed class MqttPersistence_Dispose
{
    [Fact]
    public void Disposes_client()
    {
        using MqttPersistenceContext context = new();

        context.Persistence.Dispose();

        ((IDisposable)context.MqttClient).Received(1).Dispose();
    }
}

internal sealed class MqttPersistenceContext : IDisposable
{
    internal IVirtualMqttClient MqttClient { get; }
    internal TestLogger<MqttPersistence> Logger { get; }
    internal INameResolver NameResolver { get; }
    internal MqttPersistence Persistence { get; }

    internal MqttPersistenceContext(Action<MqttCommunication>? configureCommunication = null)
    {
        MqttClient = Substitute.For<IVirtualMqttClient, IDisposable>();
        Logger = new();
        NameResolver = Substitute.For<INameResolver>();

        MqttCommunication communication = new()
        {
            QualityOfService = 2,
        };
        configureCommunication?.Invoke(communication);

        Persistence = new(communication, MqttClient, Logger, NameResolver, AssemblyLoadContext.Default);
    }

    public void Dispose()
    {
        Persistence.Dispose();
        MqttClient.Dispose();
    }
}
