using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Protocol;

namespace ViciOne.ManagedEngine.Runtime;

internal sealed class CycleInfoService : IHostedService, IDisposable
{
    private readonly IVirtualMqttClient _mqttClient;
    private readonly string _reportTopic;
    private readonly string _crashReportTopic;
    private readonly IEngineChain _engineChain;

    public CycleInfoService(IOptions<HostConfig> options, MqttOptimizer mqttOptimizer, IEngineChain engineChain, ILoggerFactory loggerFactory)
        : this(options, mqttOptimizer.Register(options.Value.Monitoring, loggerFactory), engineChain)
    {
    }

    /// <summary>
    /// For tests
    /// </summary>
    internal CycleInfoService(IOptions<HostConfig> options, IVirtualMqttClient mqttClient, IEngineChain engineChain)
    {
        _engineChain = engineChain;
        _mqttClient = mqttClient;
        _reportTopic = $"{options.Value.Id}/cycle/info";
        _crashReportTopic = $"{options.Value.Id}/cycle/crash";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _mqttClient.Connect().ConfigureAwait(false);
        _engineChain.CycleReport += SendCycleReportAsync;
        _engineChain.CrashReport += SendCrashReportAsync;
    }

    private async Task SendCycleReportAsync(EngineChainCycleReport report)
    {
        using MemoryStream payloadStream = new();
        await ReportToMessageAsync(report, payloadStream);
        await _mqttClient.Publish(new MqttApplicationMessageBuilder()
            .WithTopic(_reportTopic)
            .WithPayload(payloadStream)
            .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData)
            .WithContentType(MediaTypeNames.Application.Json)
            .Build()).ConfigureAwait(false);
    }

    private async Task SendCrashReportAsync(EngineChainCrashReport report)
    {
        using MemoryStream payloadStream = new();
        await ReportToMessageAsync(report, payloadStream);
        await _mqttClient.Publish(new MqttApplicationMessageBuilder()
            .WithTopic(_crashReportTopic)
            .WithPayload(payloadStream)
            .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData)
            .WithContentType(MediaTypeNames.Application.Json)
            .Build()).ConfigureAwait(false);
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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _engineChain.CycleReport -= SendCycleReportAsync;
        _engineChain.CrashReport -= SendCrashReportAsync;
        await _mqttClient.Disconnect().ConfigureAwait(false);
    }

    public void Dispose() => _mqttClient.Dispose();
}
