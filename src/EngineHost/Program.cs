using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Settings.Configuration;
using Serilog.Sinks.Journal;
using Testably.Abstractions;
using ViciOne.ManagedEngine;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.PackageResolver;
using ViciOne.ManagedEngine.Pipelines;
using ViciOne.ManagedEngine.Pipelines.Requests;
using ViciOne.ManagedEngine.Runtime;

if (RegardVersionArguments(args))
    return;

await CreateHost(args).RunAsync();

static bool RegardVersionArguments(string[] args)
{
    if (args.Contains("--version"))
    {
        Console.WriteLine(EngineHost.Version);
        return true;
    }

    if (args.Contains("--info"))
    {
        Console.WriteLine(EngineHost.InformationalVersion);
        return true;
    }

    return false;
}

static IHost CreateHost(string[] args)
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.AddOpenTelemetry();

    if (!builder.Environment.IsDevelopment())
    {
        builder.Services
            .AddSerilog(serilog =>
            {
                var configurationAssemblies = new[]
                {
                    typeof(ConsoleLoggerConfigurationExtensions).Assembly,
                    typeof(FileLoggerConfigurationExtensions).Assembly,
                    typeof(SyslogLoggerConfigurationExtensions).Assembly,
                    typeof(JournalSinkExtensions).Assembly,
                };
                serilog.ReadFrom.Configuration(builder.Configuration, new ConfigurationReaderOptions(configurationAssemblies))
                    .Enrich.FromLogContext();
            });
    }

    builder.Services
        .AddHostConfig("mqtt")
        .AddMqttClients()
        .AddSingleton<IContextPool, ContextPool>()
        .AddSingleton<IDeploymentPool, DeploymentPool>()
        .AddSingleton<IEngineChain, EngineChain>()
        .AddSingleton<TransactionContext>()
        .AddSingleton<IFileSystem, RealFileSystem>()
        .AddSingleton<JsonSerializerOptionsCache>()
        .AddHostedService<LifetimeService>()
        .AddHostedService<DeploymentsService>()
        .AddHostedService<CycleInfoService>()
        .AddHostedService<TerminateFileObserver>()
        .AddMediator()
        .AddMqttPipelineCommunicationAdapter(methods =>
        {
            methods.Register<DeployEnginePayload, DeployEngine, string>("Deploy", CommunicationPayloadConverters.ToDeployEngine);
            methods.Register<string, StartEngine, bool>("Start", CommunicationPayloadConverters.ToStartEngine);
            methods.Register<string, StopEngine, bool>("Stop", CommunicationPayloadConverters.ToStopEngine);
            methods.Register<string, TearDownEngine, bool>("TearDown", CommunicationPayloadConverters.ToTearDownEngine);
            methods.Register<string, GetState, EngineState>("GetState", CommunicationPayloadConverters.ToGetState);
            methods.Register<string, GetPackages, IReadOnlyCollection<PackageReference>>("GetPackages", CommunicationPayloadConverters.ToGetPackages);
            methods.Register<GetDeploymentStates, IReadOnlyDictionary<string, EngineState>>("GetDeploymentStates");
        });
    return builder.Build();
}
