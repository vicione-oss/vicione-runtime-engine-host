using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQTTnet.Extensions;
using MQTTnet.Formatter;

namespace ViciOne.ManagedEngine.Communication;

public static class MqttSetup
{
    private static readonly Guid s_clientGuid = Guid.NewGuid();

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

    public static MqttRpcServer CreateRpcServer(IVirtualMqttClient mqttClient, string topic, string fallbackTopic, ILogger logger, MqttRpcRegistrations requests)
        => new(mqttClient, topic, fallbackTopic, logger, requests, (v, t, s) => JsonSerializer.Serialize(s, v, t), (s, t) => JsonSerializer.Deserialize(s, t));
}
