using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Protocol;
using NSubstitute;
using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

public sealed class CycleInfoService_StartAsync
{
    [Fact]
    public async Task Connects_MQTT_client()
    {
        CycleInfoServiceContext context = new();

        await context.Service.StartAsync(CancellationToken.None);

        await context.MqttClient.Received(1).Connect();
    }

    [Fact]
    public async Task Starts_sending_cycle_reports()
    {
        CycleInfoServiceContext context = new();
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
        CycleInfoServiceContext context = new();
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

public sealed class CycleInfoService_ConcurrencyGuard
{
    [Fact]
    public async Task Skips_cycle_report_when_previous_is_still_in_progress()
    {
        ConcurrencyTestContext context = new();
        await context.Service.StartAsync(CancellationToken.None);

        var publishBlocker = new TaskCompletionSource();
        var publishCallCount = 0;
        context.MqttClient.Publish(Arg.Any<MqttApplicationMessage>())
            .ReturnsForAnyArgs(async _ =>
            {
                Interlocked.Increment(ref publishCallCount);
                await publishBlocker.Task;
            });

        EngineChainCycleReport report = new(1.Seconds(), new Dictionary<string, ChainLinkReport>() { { "123", new ChainLinkReport(1, 100.Milliseconds()) } });

        // First report starts and blocks on publish
        var firstReport = context.EngineChain.RaiseCycleReport(report);

        // Wait for the first publish to actually start
        while (Volatile.Read(ref publishCallCount) == 0)
            await Task.Yield();

        // Second report should be skipped because the first is still in progress
        var secondReport = context.EngineChain.RaiseCycleReport(report);
        await secondReport;

        // Unblock first report
        publishBlocker.SetResult();
        await firstReport;

        await context.MqttClient.Received(1).Publish(Arg.Any<MqttApplicationMessage>());
    }

    [Fact]
    public async Task Logs_warning_when_cycle_report_is_skipped()
    {
        ConcurrencyTestContext context = new();
        await context.Service.StartAsync(CancellationToken.None);

        var publishBlocker = new TaskCompletionSource();
        var publishCallCount = 0;
        context.MqttClient.Publish(Arg.Any<MqttApplicationMessage>())
            .ReturnsForAnyArgs(async _ =>
            {
                Interlocked.Increment(ref publishCallCount);
                await publishBlocker.Task;
            });

        EngineChainCycleReport report = new(1.Seconds(), new Dictionary<string, ChainLinkReport>() { { "123", new ChainLinkReport(1, 100.Milliseconds()) } });

        var firstReport = context.EngineChain.RaiseCycleReport(report);

        while (Volatile.Read(ref publishCallCount) == 0)
            await Task.Yield();

        await context.EngineChain.RaiseCycleReport(report);

        publishBlocker.SetResult();
        await firstReport;

        context.Logger.Entries.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { LogLevel = LogLevel.Warning, Message = "Skipped sending cycle report because the previous report is still being sent.", EventId = new EventId(1) });
    }

    [Fact]
    public async Task Logs_cycle_report_skip_warning_only_once_per_burst()
    {
        ConcurrencyTestContext context = new();
        await context.Service.StartAsync(CancellationToken.None);

        var publishBlocker = new TaskCompletionSource();
        var publishCallCount = 0;
        context.MqttClient.Publish(Arg.Any<MqttApplicationMessage>())
            .ReturnsForAnyArgs(async _ =>
            {
                Interlocked.Increment(ref publishCallCount);
                await publishBlocker.Task;
            });

        EngineChainCycleReport report = new(1.Seconds(), new Dictionary<string, ChainLinkReport>() { { "123", new ChainLinkReport(1, 100.Milliseconds()) } });

        var firstReport = context.EngineChain.RaiseCycleReport(report);

        while (Volatile.Read(ref publishCallCount) == 0)
            await Task.Yield();

        // Multiple skips should produce only one log entry
        await context.EngineChain.RaiseCycleReport(report);
        await context.EngineChain.RaiseCycleReport(report);
        await context.EngineChain.RaiseCycleReport(report);

        publishBlocker.SetResult();
        await firstReport;

        context.Logger.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task Resets_cycle_report_skip_log_after_successful_send()
    {
        ConcurrencyTestContext context = new();
        await context.Service.StartAsync(CancellationToken.None);

        var publishBlocker = new TaskCompletionSource();
        var publishCallCount = 0;
        context.MqttClient.Publish(Arg.Any<MqttApplicationMessage>())
            .ReturnsForAnyArgs(async _ =>
            {
                Interlocked.Increment(ref publishCallCount);
                await publishBlocker.Task;
            });

        EngineChainCycleReport report = new(1.Seconds(), new Dictionary<string, ChainLinkReport>() { { "123", new ChainLinkReport(1, 100.Milliseconds()) } });

        // First burst: one skip
        var firstReport = context.EngineChain.RaiseCycleReport(report);
        while (Volatile.Read(ref publishCallCount) == 0)
            await Task.Yield();
        await context.EngineChain.RaiseCycleReport(report);
        publishBlocker.SetResult();
        await firstReport;

        // Second burst: another skip should log again because the flag was reset
        var publishBlocker2 = new TaskCompletionSource();
        context.MqttClient.Publish(Arg.Any<MqttApplicationMessage>())
            .ReturnsForAnyArgs(async _ =>
            {
                Interlocked.Increment(ref publishCallCount);
                await publishBlocker2.Task;
            });

        var secondReport = context.EngineChain.RaiseCycleReport(report);
        while (Volatile.Read(ref publishCallCount) == 1)
            await Task.Yield();
        await context.EngineChain.RaiseCycleReport(report);
        publishBlocker2.SetResult();
        await secondReport;

        context.Logger.Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task Skips_crash_report_when_previous_is_still_in_progress()
    {
        ConcurrencyTestContext context = new();
        await context.Service.StartAsync(CancellationToken.None);

        var publishBlocker = new TaskCompletionSource();
        var publishCallCount = 0;
        context.MqttClient.Publish(Arg.Any<MqttApplicationMessage>())
            .ReturnsForAnyArgs(async _ =>
            {
                Interlocked.Increment(ref publishCallCount);
                await publishBlocker.Task;
            });

        EngineChainCrashReport report = new(["Exception Message"]);

        // First report starts and blocks on publish
        var firstReport = context.EngineChain.RaiseCrashReport(report);

        // Wait for the first publish to actually start
        while (Volatile.Read(ref publishCallCount) == 0)
            await Task.Yield();

        // Second report should be skipped because the first is still in progress
        var secondReport = context.EngineChain.RaiseCrashReport(report);
        await secondReport;

        // Unblock first report
        publishBlocker.SetResult();
        await firstReport;

        await context.MqttClient.Received(1).Publish(Arg.Any<MqttApplicationMessage>());
    }

    [Fact]
    public async Task Logs_warning_when_crash_report_is_skipped()
    {
        ConcurrencyTestContext context = new();
        await context.Service.StartAsync(CancellationToken.None);

        var publishBlocker = new TaskCompletionSource();
        var publishCallCount = 0;
        context.MqttClient.Publish(Arg.Any<MqttApplicationMessage>())
            .ReturnsForAnyArgs(async _ =>
            {
                Interlocked.Increment(ref publishCallCount);
                await publishBlocker.Task;
            });

        EngineChainCrashReport report = new(["Exception Message"]);

        var firstReport = context.EngineChain.RaiseCrashReport(report);

        while (Volatile.Read(ref publishCallCount) == 0)
            await Task.Yield();

        await context.EngineChain.RaiseCrashReport(report);

        publishBlocker.SetResult();
        await firstReport;

        context.Logger.Entries.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { LogLevel = LogLevel.Warning, Message = "Skipped sending crash report because the previous report is still being sent.", EventId = new EventId(2) });
    }

    [Fact]
    public async Task Logs_crash_report_skip_warning_only_once_per_burst()
    {
        ConcurrencyTestContext context = new();
        await context.Service.StartAsync(CancellationToken.None);

        var publishBlocker = new TaskCompletionSource();
        var publishCallCount = 0;
        context.MqttClient.Publish(Arg.Any<MqttApplicationMessage>())
            .ReturnsForAnyArgs(async _ =>
            {
                Interlocked.Increment(ref publishCallCount);
                await publishBlocker.Task;
            });

        EngineChainCrashReport report = new(["Exception Message"]);

        var firstReport = context.EngineChain.RaiseCrashReport(report);

        while (Volatile.Read(ref publishCallCount) == 0)
            await Task.Yield();

        await context.EngineChain.RaiseCrashReport(report);
        await context.EngineChain.RaiseCrashReport(report);
        await context.EngineChain.RaiseCrashReport(report);

        publishBlocker.SetResult();
        await firstReport;

        context.Logger.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task Resets_crash_report_skip_log_after_successful_send()
    {
        ConcurrencyTestContext context = new();
        await context.Service.StartAsync(CancellationToken.None);

        var publishBlocker = new TaskCompletionSource();
        var publishCallCount = 0;
        context.MqttClient.Publish(Arg.Any<MqttApplicationMessage>())
            .ReturnsForAnyArgs(async _ =>
            {
                Interlocked.Increment(ref publishCallCount);
                await publishBlocker.Task;
            });

        EngineChainCrashReport report = new(["Exception Message"]);

        var firstReport = context.EngineChain.RaiseCrashReport(report);
        while (Volatile.Read(ref publishCallCount) == 0)
            await Task.Yield();
        await context.EngineChain.RaiseCrashReport(report);
        publishBlocker.SetResult();
        await firstReport;

        var publishBlocker2 = new TaskCompletionSource();
        context.MqttClient.Publish(Arg.Any<MqttApplicationMessage>())
            .ReturnsForAnyArgs(async _ =>
            {
                Interlocked.Increment(ref publishCallCount);
                await publishBlocker2.Task;
            });

        var secondReport = context.EngineChain.RaiseCrashReport(report);
        while (Volatile.Read(ref publishCallCount) == 1)
            await Task.Yield();
        await context.EngineChain.RaiseCrashReport(report);
        publishBlocker2.SetResult();
        await secondReport;

        context.Logger.Entries.Should().HaveCount(2);
    }
}

public sealed class CycleInfoService_StopAsync
{
    [Fact]
    public async Task Disconnects_MQTT_client()
    {
        CycleInfoServiceContext context = new();

        await context.Service.StopAsync(CancellationToken.None);

        await context.MqttClient.Received(1).Disconnect();
    }

    [Fact]
    public async Task Stops_sending_reports()
    {
        CycleInfoServiceContext context = new();
        await context.Service.StartAsync(CancellationToken.None);

        await context.Service.StopAsync(CancellationToken.None);

        await context.MqttClient.DidNotReceiveWithAnyArgs().Publish(default!);
    }
}

internal sealed class CycleInfoServiceContext
{
    internal HostConfig HostConfig { get; } = new() { Id = "test", };
    internal IVirtualMqttClient MqttClient { get; } = Substitute.For<IVirtualMqttClient>();
    internal IEngineChain EngineChain { get; } = Substitute.For<IEngineChain>();
    internal CycleInfoService Service { get; }

    internal CycleInfoServiceContext()
    {
        var logger = Substitute.For<ILogger<CycleInfoService>>();
        var options = Substitute.For<IOptions<HostConfig>>();
        options.Value.Returns(HostConfig);
        Service = new(options, MqttClient, EngineChain, logger);
    }
}

internal sealed class ConcurrencyTestContext
{
    internal HostConfig HostConfig { get; } = new() { Id = "test", };
    internal IVirtualMqttClient MqttClient { get; } = Substitute.For<IVirtualMqttClient>();
    internal FakeEngineChain EngineChain { get; } = new();
    internal TestLogger<CycleInfoService> Logger { get; } = new();
    internal CycleInfoService Service { get; }

    internal ConcurrencyTestContext()
    {
        var options = Substitute.For<IOptions<HostConfig>>();
        options.Value.Returns(HostConfig);
        Service = new(options, MqttClient, EngineChain, Logger);
    }
}

internal sealed class FakeEngineChain : IEngineChain
{
    public event Func<EngineChainCycleReport, Task>? CycleReport;
    public event Func<EngineChainCrashReport, Task>? CrashReport;

    internal Task RaiseCycleReport(EngineChainCycleReport report) => CycleReport?.Invoke(report) ?? Task.CompletedTask;
    internal Task RaiseCrashReport(EngineChainCrashReport report) => CrashReport?.Invoke(report) ?? Task.CompletedTask;

    public Task StopAsync() => Task.CompletedTask;
    public Task SetUpCycleTimeAsync(TimeSpan cycleTime) => Task.CompletedTask;
    public void AddChainLink(string id, Func<ulong> chainLink, int index) { }
    public Task RemoveChainLinkAsync(string id) => Task.CompletedTask;
    public void EnableChainLink(string id) { }
    public void DisableChainLink(string id) { }
}
