using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using NSubstitute;
using ViciOne.ManagedEngine.Pipelines;
using Xunit;

namespace ViciOne.ManagedEngine.Communication;

public sealed class MqttPipelineCommunicationAdapter_StartAsync
{
    [Fact]
    public async Task Subscribes_correct_topic_Async()
    {
        MqttPipelineCommunicationAdapterContext context = new();
        context.SubscribeRequestTopicSuccessful();
        context.HostConfig.Value.Returns(new HostConfig() { Id = "123" });

        await context.Adapter.StartAsync(CancellationToken.None);

        await context.Adapter.StopAsync(CancellationToken.None);
        await context.MqttClient.Received().Connect();
        context.MqttClient.Received().MessageReceived += Arg.Any<Func<MqttApplicationMessageReceivedEventArgs, Task>>();
        await context.MqttClient.Received().Subscribe("123/request", MqttQualityOfServiceLevel.ExactlyOnce, false);
    }

    [Fact]
    public async Task Throws_if_subscribe_timed_out()
    {
        MqttPipelineCommunicationAdapterContext context = new();
        context.MqttClient
            .When(c => c.Subscribe(Arg.Any<string>(), Arg.Any<MqttQualityOfServiceLevel>(), Arg.Any<bool>()))
            .Do(_ => context.TimeProvider.Advance(11.Seconds()));

        var act = FluentActions.Awaiting(() => context.Adapter.StartAsync(CancellationToken.None));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

public sealed class MqttPipelineCommunicationAdapter_StopAsync
{
    [Fact]
    public async Task Unsubscribes_topic_Async()
    {
        MqttPipelineCommunicationAdapterContext context = new();
        context.SubscribeRequestTopicSuccessful();
        context.HostConfig.Value.Returns(new HostConfig() { Id = "123" });
        await context.Adapter.StartAsync(CancellationToken.None);

        await context.Adapter.StopAsync(CancellationToken.None);

        await context.MqttClient.Received().Unsubscribe("123/request");
        await context.MqttClient.Received().Disconnect();
    }
}

public sealed class MqttPipelineCommunicationAdapter_OnMessageReceivedAsync : IAsyncLifetime
{
    private readonly MqttPipelineCommunicationAdapterContext _context = new();

    public async ValueTask InitializeAsync()
    {
        _context.SubscribeRequestTopicSuccessful();
        await _context.Adapter.StartAsync(CancellationToken.None);
    }

    public async ValueTask DisposeAsync() => await _context.Adapter.StopAsync(CancellationToken.None);

    [Fact]
    public async Task Can_handle_query_Async()
    {
        _context.Methods.Register<string, QueryRequest, string>("Test", p =>
        {
            p.Should().Be("Text");
            return new();
        });
        _context.Mediator.Send<QueryRequest, string>(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>()).Returns("Result");
        var correlationToken = Guid.NewGuid().ToByteArray();

        var eventArgs = CreateEventArgs("Test", "\"Text\"", correlationToken);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(eventArgs);

        await _context.MqttClient.Received().Publish(Arg.Is<MqttApplicationMessage>(m =>
            m.CorrelationData.SequenceEqual(correlationToken) &&
            m.Topic == "responseTopic" &&
            Encoding.UTF8.GetString(m.UserProperties[0].ValueBuffer.ToArray()) == "ok" &&
            m.ConvertPayloadToString() == "\"Result\"" &&
            m.PayloadFormatIndicator == MqttPayloadFormatIndicator.CharacterData &&
            m.ContentType == MediaTypeNames.Application.Json));
    }

    [Fact]
    public async Task Can_handle_failed_request_Async()
    {
        _context.Methods.Register<string, QueryRequest, string>("Test", p => new());
        var correlationToken = Guid.NewGuid().ToByteArray();
        _context.Mediator.When(m => m.Send<QueryRequest, string>(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>()))
            .Throw(new IOException("cannot find engine.exe"));

        var eventArgs = CreateEventArgs("Test", "\"Text\"", correlationToken);
        _context.MqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(eventArgs);

        await _context.MqttClient.Received().Publish(Arg.Any<MqttApplicationMessage>());
        await _context.MqttClient.Received().Publish(Arg.Is<MqttApplicationMessage>(m =>
            m.CorrelationData.SequenceEqual(correlationToken) &&
            m.Topic == "responseTopic" &&
            Encoding.UTF8.GetString(m.UserProperties[0].ValueBuffer.ToArray()) == "failed" &&
            m.ConvertPayloadToString().Contains("cannot find engine.exe") &&
            m.PayloadFormatIndicator == MqttPayloadFormatIndicator.CharacterData &&
            m.ContentType == MediaTypeNames.Text.Plain));
    }

    internal sealed class QueryRequest : IRequest<string> { }

    private static MqttApplicationMessageReceivedEventArgs CreateEventArgs(string method, string payload, byte[] correlationToken)
        => new(
            "123",
            new MqttApplicationMessageBuilder()
                .WithTopic("/request")
                .WithUserProperty("Method", Encoding.UTF8.GetBytes(method))
                .WithPayload(payload)
                .WithContentType(MediaTypeNames.Application.Json)
                .WithResponseTopic("responseTopic")
                .WithCorrelationData(correlationToken)
                .Build(),
            new(),
            (_, _) => Task.CompletedTask);
}

internal sealed class MqttPipelineCommunicationAdapterContext
{
    internal IOptions<HostConfig> HostConfig { get; } = Substitute.For<IOptions<HostConfig>>();
    internal CommunicationMethods Methods { get; }
    internal IMediator Mediator { get; } = Substitute.For<IMediator>();
    internal IVirtualMqttClient MqttClient { get; } = Substitute.For<IVirtualMqttClient>();
    internal MqttPipelineCommunicationAdapter Adapter { get; }
    internal FakeTimeProvider TimeProvider { get; } = new();

    public MqttPipelineCommunicationAdapterContext()
    {
        Methods = new(Mediator);
        Adapter = new(HostConfig, Methods, MqttClient, Substitute.For<ILogger<MqttPipelineCommunicationAdapter>>(),
            TimeProvider);
    }

    internal void SubscribeRequestTopicSuccessful()
    {
        MqttClient
            .When(c => c.Subscribe(Arg.Any<string>(), Arg.Any<MqttQualityOfServiceLevel>(), Arg.Any<bool>()))
            .Do(_ => InvokeSubscribed());

        void InvokeSubscribed()
            => MqttClient.InnerClient.SubscriptionsChangedAsync += Raise.Event<Func<SubscriptionsChangedEventArgs, Task>>(
                new SubscriptionsChangedEventArgs(
                    [
                        new MqttClientSubscribeResult(
                            0,
                            [
                                new MqttClientSubscribeResultItem(new MqttTopicFilter { Topic = $"{HostConfig.Value.Id}/request" }, MqttClientSubscribeResultCode.GrantedQoS2),
                            ],
                            "OK",
                            [])
                    ],
                    []));
    }
}
