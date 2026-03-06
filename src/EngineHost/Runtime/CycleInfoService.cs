using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Protocol;
using ViciOne.ManagedEngine.Communication;

namespace ViciOne.ManagedEngine.Runtime;

internal sealed class CycleInfoService(IOptions<HostConfig> options, [FromKeyedServices(MqttConnectionKeys.Monitoring)] IVirtualMqttClient mqttClient,
    IEngineChain engineChain, ILogger<CycleInfoService> logger) : IHostedLifecycleService
{
    private static readonly RecyclableMemoryStreamManager s_streamManager = new();
    private readonly string _reportTopic = $"{options.Value.Id}/cycle/info";
    private readonly string _crashReportTopic = $"{options.Value.Id}/cycle/crash";
    private int _cycleReportRunning;
    private int _crashReportRunning;
    private int _cycleReportSkipLogged;
    private int _crashReportSkipLogged;

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await mqttClient.Connect().ConfigureAwait(false);
        engineChain.CycleReport += SendCycleReportAsync;
        engineChain.CrashReport += SendCrashReportAsync;
    }

    private async Task SendCycleReportAsync(EngineChainCycleReport report)
    {
        if (Interlocked.CompareExchange(ref _cycleReportRunning, 1, 0) != 0)
        {
            if (Interlocked.CompareExchange(ref _cycleReportSkipLogged, 1, 0) == 0)
                logger.CycleReportSkipped();

            return;
        }

        try
        {
            using var payloadStream = s_streamManager.GetStream();
            await ReportToMessageAsync(report, payloadStream);
            await mqttClient.Publish(new MqttApplicationMessageBuilder()
                .WithTopic(_reportTopic)
                .WithPayload(payloadStream)
                .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData)
                .WithContentType(MediaTypeNames.Application.Json)
                .Build()).ConfigureAwait(false);
            Volatile.Write(ref _cycleReportSkipLogged, 0);
        }
        finally
        {
            Volatile.Write(ref _cycleReportRunning, 0);
        }
    }

    private async Task SendCrashReportAsync(EngineChainCrashReport report)
    {
        if (Interlocked.CompareExchange(ref _crashReportRunning, 1, 0) != 0)
        {
            if (Interlocked.CompareExchange(ref _crashReportSkipLogged, 1, 0) == 0)
                logger.CrashReportSkipped();

            return;
        }

        try
        {
            using var payloadStream = s_streamManager.GetStream();
            await ReportToMessageAsync(report, payloadStream);
            await mqttClient.Publish(new MqttApplicationMessageBuilder()
                .WithTopic(_crashReportTopic)
                .WithPayload(payloadStream)
                .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData)
                .WithContentType(MediaTypeNames.Application.Json)
                .Build()).ConfigureAwait(false);
            Volatile.Write(ref _crashReportSkipLogged, 0);
        }
        finally
        {
            Volatile.Write(ref _crashReportRunning, 0);
        }
    }

    private static async Task ReportToMessageAsync(EngineChainCycleReport report, Stream stream)
    {
        var message = new InfoMessage(Math.Round(report.Duration.TotalMilliseconds, 3), [.. report.ChainLinkReports.Select(e => new DeploymentDuration(e.Key, Math.Round(e.Value.Duration.TotalMilliseconds, 3), e.Value.Cycle))]);
        await JsonSerializer.SerializeAsync(stream, message, SourceGenerationContext.Default.InfoMessage);
        stream.Position = 0;
    }

    private static async Task ReportToMessageAsync(EngineChainCrashReport report, Stream stream)
    {
        await JsonSerializer.SerializeAsync(stream, new CrashMessage(report.CrashedChainLinks), SourceGenerationContext.Default.CrashMessage);
        stream.Position = 0;
    }

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        engineChain.CycleReport -= SendCycleReportAsync;
        engineChain.CrashReport -= SendCrashReportAsync;
        await mqttClient.Disconnect().ConfigureAwait(false);
    }

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
