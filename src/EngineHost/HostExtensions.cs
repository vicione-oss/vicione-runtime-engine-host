using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Extensions;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Pipelines;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine;

internal static class HostExtensions
{
    private const string OtelEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string OtelAdditionalMeters = "OTEL_ADDITIONAL_METERS";
    private const string OtelAdditionalSources = "OTEL_ADDITIONAL_SOURCES";

    internal static TBuilder AddOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration[OtelEndpoint]);
        if (useOtlpExporter)
        {
            if (builder.Environment.IsDevelopment())
            {
                builder.Logging.AddOpenTelemetry(logging =>
                {
                    logging.IncludeFormattedMessage = true;
                    logging.IncludeScopes = true;
                });
            }

            var openTelemetry = builder.Services.AddOpenTelemetry()
                .ConfigureResource(b =>
                    b.AddService(
                        builder.Environment.ApplicationName,
                        serviceNamespace: "vicione",
                        serviceVersion: EngineHost.Version,
                        serviceInstanceId: builder.Configuration.GetValue<string>(nameof(HostConfig.Id)) ?? "unknown",
                        autoGenerateServiceInstanceId: false)
                .AddAttributes(
                [
                    new("process.pid", Environment.ProcessId),
                ]))
                .WithMetrics(metrics =>
                {
                    metrics.AddRuntimeInstrumentation();
                    metrics.AddMeter(EngineChainTelemetry.MeterName);

                    var additionalMeters = builder.Configuration.GetSection(OtelAdditionalMeters).Get<string[]>() ?? [];
                    foreach (var meter in additionalMeters)
                        metrics.AddMeter(meter);
                })
                .WithTracing(tracing =>
                {
                    tracing.AddSource(EngineChainTelemetry.ActivitySourceName);

                    var additionalSources = builder.Configuration.GetSection(OtelAdditionalSources).Get<string[]>() ?? [];
                    foreach (var source in additionalSources)
                        tracing.AddSource(source);
                });

            openTelemetry.UseOtlpExporter();
        }
        return builder;
    }

    internal static IServiceCollection AddHostConfig(this IServiceCollection services, string connectionName)
    {
        services
            .AddOptions<HostConfig>()
            .Configure<IConfiguration>((hostConfig, configuration) => HostConfig.Apply(hostConfig, configuration, connectionName));
        return services;
    }

    internal static IServiceCollection AddMqttClients(this IServiceCollection services)
    {
        services.AddKeyedSingleton(
            MqttConnectionKeys.CommandBus,
            (sp, _) =>
            {
                var config = sp.GetRequiredService<IOptions<HostConfig>>();
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                return MqttOptimizer.Instance.Register(config.Value.CommandBus, loggerFactory);
            });

        services.AddKeyedSingleton(
            MqttConnectionKeys.Monitoring,
            (sp, _) =>
            {
                var config = sp.GetRequiredService<IOptions<HostConfig>>();
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                return MqttOptimizer.Instance.Register(config.Value.Monitoring, loggerFactory);
            });

        return services;
    }

    internal static IServiceCollection AddMqttPipelineCommunicationAdapter(this IServiceCollection services,
        Action<CommunicationMethods> configure)
    {
        services
            .AddHostedService<MqttPipelineCommunicationAdapter>()
            .AddSingleton(s =>
            {
                CommunicationMethods methods = new(s.GetRequiredService<IMediator>());
                configure(methods);
                return methods;
            });
        return services;
    }
}
