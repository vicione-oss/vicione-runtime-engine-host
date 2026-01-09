using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Protocol;
using NSubstitute;
using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

public sealed class CycleInfoService_Start
{
    [Fact]
    public async Task Connects_MQTT_client()
    {
        using CycleInfoServiceContext context = new();

        await context.Service.StartAsync(CancellationToken.None);

        await context.MqttClient.Received(1).Connect();
    }

    [Fact]
    public async Task Starts_sending_cycle_reports()
    {
        using CycleInfoServiceContext context = new();
        await context.Service.StartAsync(CancellationToken.None);
        var messages = new List<MqttApplicationMessage>();
        await context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        EngineChainCycleReport report = new(1.Seconds(), new Dictionary<string, ChainLinkReport>() { { "123", new ChainLinkReport(456, 1.Minutes()) }, });
        context.EngineChain.CycleReport += Raise.Event<Func<EngineChainCycleReport, Task>>(report);

        var message = messages.Should().ContainSingle().Subject;
        message.ConvertPayloadToString().Should().Be("""{"CycleDuration":1000,"Deployments":[{"Deployment":"123","Duration":60000,"Cycle":456}]}""");
        message.Topic.Should().Be($"{context.HostConfig.Id}/cycle/info");
        message.TopicAlias.Should().Be(0);
        message.ResponseTopic.Should().BeNull();
        message.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
        message.Retain.Should().BeFalse();
        message.PayloadFormatIndicator.Should().Be(MqttPayloadFormatIndicator.CharacterData);
        message.ContentType.Should().Be(MediaTypeNames.Application.Json);
        message.UserProperties.Should().BeNull();
    }

    [Fact]
    public async Task Starts_sending_crash_reports()
    {
        using CycleInfoServiceContext context = new();
        await context.Service.StartAsync(CancellationToken.None);
        var messages = new List<MqttApplicationMessage>();
        await context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        EngineChainCrashReport report = new(["Exception Message"]);
        context.EngineChain.CrashReport += Raise.Event<Func<EngineChainCrashReport, Task>>(report);

        var message = messages.Should().ContainSingle().Subject;
        message.ConvertPayloadToString().Should().Be("""{"Deployments":["Exception Message"]}""");
        message.Topic.Should().Be($"{context.HostConfig.Id}/cycle/crash");
        message.TopicAlias.Should().Be(0);
        message.ResponseTopic.Should().BeNull();
        message.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
        message.Retain.Should().BeFalse();
        message.PayloadFormatIndicator.Should().Be(MqttPayloadFormatIndicator.CharacterData);
        message.ContentType.Should().Be(MediaTypeNames.Application.Json);
        message.UserProperties.Should().BeNull();
    }
}

public sealed class CycleInfoService_Stop
{
    [Fact]
    public async Task Disconnects_MQTT_client()
    {
        using CycleInfoServiceContext context = new();

        await context.Service.StopAsync(CancellationToken.None);

        await context.MqttClient.Received(1).Disconnect();
    }

    [Fact]
    public async Task Stops_sending_reports()
    {
        using CycleInfoServiceContext context = new();
        await context.Service.StartAsync(CancellationToken.None);

        await context.Service.StopAsync(CancellationToken.None);

        await context.MqttClient.DidNotReceiveWithAnyArgs().Publish(default!);
    }
}

internal sealed class CycleInfoServiceContext : IDisposable
{
    internal HostConfig HostConfig { get; } = new() { Id = "test", };
    internal IVirtualMqttClient MqttClient { get; } = Substitute.For<IVirtualMqttClient>();
    internal IEngineChain EngineChain { get; } = Substitute.For<IEngineChain>();
    internal CycleInfoService Service { get; }

    internal CycleInfoServiceContext()
    {
        var options = Substitute.For<IOptions<HostConfig>>();
        options.Value.Returns(HostConfig);
        Service = new(options, MqttClient, EngineChain);
    }

    public void Dispose() => Service.Dispose();
}
