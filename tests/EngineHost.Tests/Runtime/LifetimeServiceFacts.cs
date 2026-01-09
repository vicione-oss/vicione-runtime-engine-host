using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Protocol;
using NSubstitute;
using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

public sealed class LifetimeService_Start
{
    [Fact]
    public async Task Connects_MQTT_client()
    {
        using LifetimeServiceContext context = new();

        await context.Service.StartAsync(CancellationToken.None);

        await context.MqttClient.Received(1).Connect();
    }
}

public sealed class LifetimeService_ApplicationStarted
{
    [Fact]
    public async Task Sends_up_and_running_state_on_application_started()
    {
        using LifetimeServiceContext context = new();
        await context.Service.StartAsync(CancellationToken.None);
        var messages = new List<MqttApplicationMessage>();
        await context.MqttClient.Publish(Arg.Do<MqttApplicationMessage>(messages.Add));

        await context.CancellationTokenSource.CancelAsync();

        var message = messages.Should().ContainSingle().Subject;
        message.ConvertPayloadToString().Should().Be("upandrunning");
        message.Topic.Should().Be($"{context.HostConfig.Id}/state");
        message.TopicAlias.Should().Be(0);
        message.ResponseTopic.Should().BeNull();
        message.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
        message.Retain.Should().BeFalse();
        message.PayloadFormatIndicator.Should().Be(MqttPayloadFormatIndicator.CharacterData);
        message.ContentType.Should().Be(MediaTypeNames.Text.Plain);
        var userProperty = message.UserProperties.Should().ContainSingle().Subject;
        userProperty.Name.Should().Be("Version");
        userProperty.Value.Should().Be(EngineHost.InformationalVersion);
        context.Logger.Calls.Should().Be(1);
        context.Logger.LogLevel.Should().Be(LogLevel.Information);
        context.Logger.EventId.Should().Be(new EventId(0, nameof(LifetimeServiceLog.UpAndRunning)));
        context.Logger.Exception.Should().BeNull();
        context.Logger.Message.Should().MatchEquivalentOf($"*{EngineHost.InformationalVersion}*up*running*");
    }
}

public sealed class LifetimeService_ConfigureLastWill
{
    [Fact]
    public void Configures_last_will()
    {
        using LifetimeServiceContext context = new();
        CommunicationInfo communicationInfo = new();

        LifetimeService.ConfigureLastWill(context.HostConfig, communicationInfo);

        communicationInfo.WillContentType.Should().Be(MediaTypeNames.Text.Plain);
        communicationInfo.WillMessage.Should().Be("interruption");
        communicationInfo.WillQualityOfService.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
        communicationInfo.WillRetain.Should().BeNull();
        communicationInfo.WillTopic.Should().Be($"{context.HostConfig.Id}/state");
    }
}

public sealed class LifetimeService_Stop
{
    [Fact]
    public async Task Disconnects_MQTT_client()
    {
        using LifetimeServiceContext context = new();

        await context.Service.StopAsync(CancellationToken.None);

        await context.MqttClient.Received(1).Disconnect();
    }
}

internal sealed class LifetimeServiceContext : IDisposable
{
    internal HostConfig HostConfig { get; } = new() { Id = "test", };
    internal IVirtualMqttClient MqttClient { get; } = Substitute.For<IVirtualMqttClient>();
    internal IHostApplicationLifetime HostApplicationLifetime { get; } = Substitute.For<IHostApplicationLifetime>();
    internal LifetimeService Service { get; }
    internal CancellationTokenSource CancellationTokenSource { get; } = new();
    internal TestLogger<LifetimeService> Logger { get; } = new();

    internal LifetimeServiceContext()
    {
        var options = Substitute.For<IOptions<HostConfig>>();
        options.Value.Returns(HostConfig);
        HostApplicationLifetime.ApplicationStarted.Returns(CancellationTokenSource.Token);
        Service = new(options, MqttClient, Logger, HostApplicationLifetime);
    }

    public void Dispose() => Service.Dispose();
}
