using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQTTnet.Extensions;
using MQTTnet.Formatter;

namespace ViciOne.ManagedEngine.Communication;

/// <summary>
/// Provides MQTT communication configuration utilities.
/// </summary>
public static class MqttSetup
{
    private static readonly Guid s_clientGuid = Guid.NewGuid();

    /// <summary>
    /// Converts MQTT communication settings to a communication info object.
    /// </summary>
    /// <param name="communication">The MQTT communication settings.</param>
    /// <param name="configure">An optional action to configure the communication info.</param>
    /// <returns>The configured communication info.</returns>
    public static CommunicationInfo ToCommunicationInfo(this MqttCommunication communication, Action<CommunicationInfo>? configure = null)
        => ToCommunicationInfo(communication, s_clientGuid, configure);

    internal static CommunicationInfo ToCommunicationInfo(this MqttCommunication communication, Guid clientId, Action<CommunicationInfo>? configure = null)
    {
        CommunicationInfo communicationInfo = new()
        {
            TcpHost = communication.Host,
            TcpPort = communication.Port,
            WebSocketUri = communication.Uri,
            ProtocolVersion = MqttProtocolVersion.V500,
            CleanSession = communication.CleanSession,
            SessionExpiryInterval = communication.SessionExpiryInterval,
            Username = communication.Username,
            Password = communication.Password,
            SslProtocol = communication.SslProtocol,
            CertificateFile = communication.CertificateFile,
            CertificateFilePassword = communication.CertificateFilePassword,
            CertificatePrivateKeyFile = communication.CertificatePrivateKeyFile,
            DisableCertificateValidation = communication.DisableCertificateValidation,
            MaxPendingMessages = communication.MaxPendingMessages,
            ClientId = $"eh-com-{clientId}",
        };
        configure?.Invoke(communicationInfo);
        return communicationInfo;
    }

    /// <summary>
    /// Creates an MQTT RPC server with JSON serialization.
    /// </summary>
    /// <param name="mqttClient">The MQTT client to use.</param>
    /// <param name="topic">The primary topic for RPC requests.</param>
    /// <param name="fallbackTopic">The fallback topic for RPC requests.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="requests">The RPC request registrations.</param>
    /// <returns>A configured MQTT RPC server.</returns>
    public static MqttRpcServer CreateRpcServer(IVirtualMqttClient mqttClient, string topic, string fallbackTopic, ILogger logger, MqttRpcRegistrations requests)
        => new(mqttClient, topic, fallbackTopic, logger, requests, (v, t, s) => JsonSerializer.Serialize(s, v, t), (s, t) => JsonSerializer.Deserialize(s, t));
}
