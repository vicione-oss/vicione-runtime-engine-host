using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using NSubstitute;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Pipelines;
using ViciOne.ManagedEngine.Pipelines.Commands;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.RuntimeFunctions;

public sealed class MqttRuntimeFunctionProvider_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
    {
        await using ConstructorProviderFactory providerFactory = new();

        var instance = providerFactory.CreateRuntimeFunctionProvider(
            Substitute.For<IRuntimeFunctionHandler>(),
            new MqttCommunication { Host = "localhost", },
            Substitute.For<ILoggerFactory>(),
            "main");

        instance.Should().NotBeNull().And.BeOfType<MqttRuntimeFunctionProvider>();
    }
}

public sealed class MqttRuntimeFunctionProvider_Connect : IDisposable
{
    private readonly MqttRuntimeFunctionProviderContext _context = new();
    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Connects_client_Async()
    {
        await _context.Provider.ConnectAsync(CancellationToken.None);

        await _context.MqttClient.Received(1).Connect();
        _context.Logger.Entries.Should().SatisfyRespectively(
            pending =>
            {
                pending.EventId.Should().Be(new EventId(1));
                pending.Exception.Should().BeNull();
                pending.LogLevel.Should().Be(LogLevel.Debug);
                pending.Message.Should().MatchEquivalentOf("*Subscribing*main/request*");
            },
            finished =>
            {
                finished.EventId.Should().Be(new EventId(2));
                finished.Exception.Should().BeNull();
                finished.LogLevel.Should().Be(LogLevel.Information);
                finished.Message.Should().MatchEquivalentOf("*Subscribed*main/request*");
            });
    }

    [Fact]
    public async Task Subscribes_engine_as_topic_Async()
    {
        await _context.Provider.ConnectAsync(CancellationToken.None);

        await _context.MqttClient.Received(1).Subscribe($"{_context.Engine}/request", MqttQualityOfServiceLevel.ExactlyOnce, false);
    }
}

public sealed class MqttRuntimeFunctionProvider_Disconnect : IDisposable
{
    private readonly MqttRuntimeFunctionProviderContext _context = new();
    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Disconnects_client_Async()
    {
        await _context.Provider.ConnectAsync(CancellationToken.None);
        _context.Logger.Entries.Clear();
        await _context.Provider.DisconnectAsync(CancellationToken.None);

        await _context.MqttClient.Received(1).Disconnect();
        _context.Logger.Entries.Should().SatisfyRespectively(
            pending =>
            {
                pending.EventId.Should().Be(new EventId(3));
                pending.LogLevel.Should().Be(LogLevel.Debug);
                pending.Message.Should().MatchEquivalentOf("*Unsubscribing*main/request*");
            },
            finished =>
            {
                finished.EventId.Should().Be(new EventId(4));
                finished.Exception.Should().BeNull();
                finished.LogLevel.Should().Be(LogLevel.Information);
                finished.Message.Should().MatchEquivalentOf("*Unsubscribed*main/request*");
            });
    }

    [Fact]
    public async Task Unsubscribes_engine_as_topic_Async()
    {
        await _context.Provider.ConnectAsync(CancellationToken.None);
        await _context.Provider.DisconnectAsync(CancellationToken.None);

        await _context.MqttClient.Received(1).Unsubscribe($"{_context.Engine}/request");
    }
}

public sealed class MqttRuntimeFunctionProvider_MessageReceived : IDisposable
{
    private const string EmptyJson = "{}";
    private readonly MqttRuntimeFunctionProviderContext _context = new();

    public MqttRuntimeFunctionProvider_MessageReceived() => _context.Provider.ConnectAsync(CancellationToken.None).GetAwaiter().GetResult();

    public void Dispose()
    {
        _context.Provider.DisconnectAsync(CancellationToken.None).GetAwaiter().GetResult();
        _context.Dispose();
    }

    [Fact]
    public async Task Throws_on_missing_command_Async()
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        var builder = new MqttApplicationMessageBuilder()
            .WithTopic($"{_context.Engine}/request")
            .WithResponseTopic("responsetopic")
            .WithUserProperty(string.Empty, string.Empty)
            .WithCorrelationData(ArrayPool<byte>.Shared.Rent(4))
            .WithJsonPayload(EmptyJson)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);
        var eventArgs = new MqttApplicationMessageReceivedEventArgs("B", builder.Build(), new(), (_, _) => Task.CompletedTask);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(eventArgs);

        var message = messages.Should().ContainSingle().Subject;
        message.Topic.Should().Be("responsetopic");
        message.TopicAlias.Should().Be(0);
        message.ResponseTopic.Should().BeNull();
        message.Retain.Should().BeFalse();
        message.PayloadFormatIndicator.Should().Be(MqttPayloadFormatIndicator.CharacterData);
        message.ContentType.Should().Be(MediaTypeNames.Text.Plain);
        message.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
        message.UserProperties.Should().BeEquivalentTo(new MqttUserProperty[]
        {
            new("Status", "failed"),
        });
        message.ConvertPayloadToString().Should().MatchEquivalentOf("*not*determine*request*name*");
    }

    [Fact]
    public async Task Throws_on_unknown_command_Async()
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        var builder = new MqttApplicationMessageBuilder()
            .WithRpc($"{_context.Engine}/request", "unknown", "responsetopic")
            .WithCorrelationData(ArrayPool<byte>.Shared.Rent(4))
            .WithJsonPayload(EmptyJson)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);
        var eventArgs = new MqttApplicationMessageReceivedEventArgs("B", builder.Build(), new(), (_, _) => Task.CompletedTask);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(eventArgs);

        var message = messages.Should().ContainSingle().Subject;
        message.Topic.Should().Be("responsetopic");
        message.TopicAlias.Should().Be(0);
        message.ResponseTopic.Should().BeNull();
        message.Retain.Should().BeFalse();
        message.PayloadFormatIndicator.Should().Be(MqttPayloadFormatIndicator.CharacterData);
        message.ContentType.Should().Be(MediaTypeNames.Text.Plain);
        message.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
        message.UserProperties.Should().BeEquivalentTo(new MqttUserProperty[]
        {
            new("Status", "failed"),
        });
        message.ConvertPayloadToString().Should().MatchEquivalentOf("*request*unknown*not*supported*");
    }

    [Fact]
    public async Task Forwards_failure_of_command_Async()
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));
        _context.Handler.Handle<ChangeLogLevelCommand, CommandStatus>(default!, default).ReturnsForAnyArgs(new Failure() { Message = "Could not set log level.", });

        var builder = new MqttApplicationMessageBuilder()
            .WithRpc($"{_context.Engine}/request", "changeloglevel", "responsetopic")
            .WithCorrelationData(ArrayPool<byte>.Shared.Rent(4))
            .WithJsonPayload(EmptyJson)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);
        var eventArgs = new MqttApplicationMessageReceivedEventArgs("B", builder.Build(), new(), (_, _) => Task.CompletedTask);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(eventArgs);

        var message = messages.Should().ContainSingle().Subject;
        message.Topic.Should().Be("responsetopic");
        message.TopicAlias.Should().Be(0);
        message.ResponseTopic.Should().BeNull();
        message.Retain.Should().BeFalse();
        message.PayloadFormatIndicator.Should().Be(MqttPayloadFormatIndicator.CharacterData);
        message.ContentType.Should().Be(MediaTypeNames.Text.Plain);
        message.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
        message.UserProperties.Should().BeEquivalentTo(new MqttUserProperty[]
        {
            new("Status", "failed"),
        });
        message.ConvertPayloadToString().Should().Be("Could not set log level.");
    }

    [Fact]
    public async Task Forwards_exception_of_command_Async()
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));
        var exception = new InvalidOperationException("No log available to set level.");
        _context.Handler.WhenForAnyArgs(_ => _.Handle<ChangeLogLevelCommand, CommandStatus>(default!, default)).Throw(exception);

        var builder = new MqttApplicationMessageBuilder()
            .WithRpc($"{_context.Engine}/request", "changeloglevel", "responsetopic")
            .WithCorrelationData(ArrayPool<byte>.Shared.Rent(4))
            .WithJsonPayload(EmptyJson)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);
        var eventArgs = new MqttApplicationMessageReceivedEventArgs("B", builder.Build(), new(), (_, _) => Task.CompletedTask);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(eventArgs);

        var message = messages.Should().ContainSingle().Subject;
        message.Topic.Should().Be("responsetopic");
        message.TopicAlias.Should().Be(0);
        message.ResponseTopic.Should().BeNull();
        message.Retain.Should().BeFalse();
        message.PayloadFormatIndicator.Should().Be(MqttPayloadFormatIndicator.CharacterData);
        message.ContentType.Should().Be(MediaTypeNames.Text.Plain);
        message.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
        message.UserProperties.Should().BeEquivalentTo(new MqttUserProperty[]
        {
            new("Status", "failed"),
        });
        message.ConvertPayloadToString().Should().Be("No log available to set level.");
    }

    [Theory]
    [MemberData(nameof(Handles_known_commands_Data))]
    public async Task Handles_known_commands_Async(string method, string payload, Type type)
    {
        var messages = new List<MqttApplicationMessage>();
        await _context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        var builder = new MqttApplicationMessageBuilder()
            .WithRpc($"{_context.Engine}/request", method, "responsetopic")
            .WithCorrelationData(ArrayPool<byte>.Shared.Rent(4))
            .WithJsonPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);
        var eventArgs = new MqttApplicationMessageReceivedEventArgs("B", builder.Build(), new(), (_, _) => Task.CompletedTask);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(eventArgs);

        var call = _context.Handler.ReceivedCalls().Should().ContainSingle().Subject;
        _ = call.GetArguments().Should().HaveCount(2).And.Subject.First().Should().BeOfType(type);

        var message = messages.Should().ContainSingle().Subject;
        message.Topic.Should().Be("responsetopic");
        message.TopicAlias.Should().Be(0);
        message.ResponseTopic.Should().BeNull();
        message.Retain.Should().BeFalse();
        message.PayloadFormatIndicator.Should().Be(MqttPayloadFormatIndicator.Unspecified);
        message.ContentType.Should().BeNull();
        message.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
        message.UserProperties.Should().BeEquivalentTo(new MqttUserProperty[]
        {
            new("Status", "ok"),
        });
        message.PayloadSegment.Array.Should().BeNull();
        _context.Logger.EventId.Should().Be(new EventId(5, "MessageReceived"));
        _context.Logger.Exception.Should().BeNull();
        _context.Logger.LogLevel.Should().Be(LogLevel.Trace);
        _context.Logger.Message.Should().MatchEquivalentOf("*received*request*from*'B'*");
    }

    public static TheoryData<string, string, Type> Handles_known_commands_Data()
        => new()
        {
            { "changeloglevel", EmptyJson, typeof(ChangeLogLevelCommand) },
            { "loadvariables", EmptyJson, typeof(LoadPersistenceVariablesCommand) },
            { "loadsettings", EmptyJson, typeof(LoadPersistenceSettingsCommand) },
        };
}

internal sealed class MqttRuntimeFunctionProviderContext : IDisposable
{
    internal string Engine { get; } = "main";

    internal IRuntimeFunctionHandler Handler { get; }
    internal TestLogger<MqttRuntimeFunctionProvider> Logger { get; }
    internal IVirtualMqttClient MqttClient { get; }
    internal MqttRuntimeFunctionProvider Provider { get; }

    internal MqttRuntimeFunctionProviderContext()
    {
        Handler = Substitute.For<IRuntimeFunctionHandler>();
        Logger = new();
        MqttClient = Substitute.For<IVirtualMqttClient, IDisposable>();
        Provider = new(Engine, Handler, Logger, MqttClient);
    }

    public void Dispose()
    {
        Provider.Dispose();
        MqttClient.Dispose();
    }
}
