using System;
using Microsoft.Extensions.Configuration;
using MQTTnet.Extensions;
using MQTTnet.Formatter;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine;

internal class HostConfig
{
    private const string TypeProperty = "Type";

    internal string Id { get; set; } = string.Empty;
    internal CommunicationInfo CommandBus { get; set; } = new();
    internal CommunicationInfo Monitoring { get; set; } = new();
    internal string DeploymentsDirectory { get; set; } = string.Empty;
    internal string PackagesDirectory { get; set; } = string.Empty;
    internal bool PreloadTypeConverterAssemblyInContext { get; set; }

    internal static void Apply(HostConfig hostConfig, IConfiguration configuration, string connectionName)
    {
        var connectionString = configuration.GetConnectionString(connectionName);
        hostConfig.Id = configuration.GetValue<string>(nameof(Id)) ?? throw ConfigurationValueNotFound(nameof(Id));
        var commandBus = configuration.GetRequiredSection(nameof(CommandBus));
        ApplyMqttConfiguration(hostConfig.CommandBus, commandBus, nameof(CommandBus), hostConfig.Id, connectionString);
        LifetimeService.ConfigureLastWill(hostConfig, hostConfig.CommandBus);
        var monitoring = configuration.GetSection(nameof(Monitoring));
        if (monitoring.Exists())
            ApplyMqttConfiguration(hostConfig.Monitoring, monitoring, nameof(Monitoring), hostConfig.Id, connectionString);
        else
            hostConfig.Monitoring = hostConfig.CommandBus;

        hostConfig.DeploymentsDirectory = configuration.GetValue<string>(nameof(DeploymentsDirectory)) ?? throw ConfigurationValueNotFound(nameof(DeploymentsDirectory));
        hostConfig.PackagesDirectory = configuration.GetValue<string>(nameof(PackagesDirectory)) ?? throw ConfigurationValueNotFound(nameof(PackagesDirectory));
        hostConfig.PreloadTypeConverterAssemblyInContext = configuration.GetValue<bool?>("PreloadTypeConverterAssemblyInContext") ?? true;
    }

    private static void ApplyMqttConfiguration(CommunicationInfo communication, IConfiguration configuration, string name, string clientId, string? connectionString)
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            communication.TcpHost = uri.Host;
            communication.TcpPort = uri.Port;
            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                var userInfo = uri.UserInfo.Split(':', 2);
                if (userInfo.Length == 2)
                {
                    communication.Username = userInfo[0];
                    communication.Password = userInfo[1];
                }
            }
        }
        else
        {
            var type = configuration.GetValue<string>(TypeProperty);
            switch (type?.ToUpperInvariant())
            {
                case "TCP":
                    communication.TcpHost = configuration.GetValue<string>("Host") ?? throw ConfigurationValueNotFound($"{name}:Host");
                    communication.TcpPort = configuration.GetValue<int>("Port");
                    break;
                case "WEBSOCKET":
                    communication.WebSocketUri = configuration.GetValue<Uri>("Url") ?? throw ConfigurationValueNotFound($"{name}:Url");
                    break;
                default: throw new NotSupportedException($"{name}:{TypeProperty} '{type}' is not supported.");
            }
            communication.Username = configuration.GetValue<string?>("Username");
            communication.Password = configuration.GetValue<string?>("Password");
        }
        communication.ProtocolVersion = MqttProtocolVersion.V500;
        communication.MaxPendingMessages = configuration.GetValue<int?>("MaxPendingMessages");
#pragma warning disable CA1308 // Normalize strings to uppercase
        communication.ClientId = $"eh-{clientId}-{name.ToLowerInvariant()}";
#pragma warning restore CA1308 // Normalize strings to uppercase
    }

    private static InvalidOperationException ConfigurationValueNotFound(string configurationPath)
        => new($"Failed to get configuration value at '{configurationPath}'.");
}
