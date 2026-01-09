using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.ExternalCommunication;

internal static partial class MqttExternalCommunicationLog
{
    internal static IDisposable ConnectionConnect(this ILogger logger, string externalConnectionId, string externalConnectionName, string address)
        => logger.CreateContext(
            _ => _.ConnectionConnecting(externalConnectionId, externalConnectionName, address),
            _ => _.ConnectionConnected(externalConnectionId, externalConnectionName, address));

    [LoggerMessage(1, LogLevel.Debug,
        "Connecting to external connection '{ExternalConnectionId}' ({ExternalConnectionName}) @ '{Address}'...")]
    internal static partial void ConnectionConnecting(this ILogger logger, string externalConnectionId, string externalConnectionName, string address);

    [LoggerMessage(2, LogLevel.Information,
        "Connected to external connection '{ExternalConnectionId}' ({ExternalConnectionName}) @ '{Address}'")]
    internal static partial void ConnectionConnected(this ILogger logger, string externalConnectionId, string externalConnectionName, string address);

    internal static IDisposable ConnectionDisconnect(this ILogger logger, string externalConnectionId, string externalConnectionName, string address)
        => logger.CreateContext(
            _ => _.ConnectionDisconnecting(externalConnectionId, externalConnectionName, address),
            _ => _.ConnectionDisconnected(externalConnectionId, externalConnectionName, address));

    [LoggerMessage(3, LogLevel.Debug,
        "Disconnecting from external connection '{ExternalConnectionId}' ({ExternalConnectionName}) @ '{Address}'...")]
    internal static partial void ConnectionDisconnecting(this ILogger logger, string externalConnectionId, string externalConnectionName, string address);

    [LoggerMessage(4, LogLevel.Information,
        "Disconnected from external connection '{ExternalConnectionId}' ({ExternalConnectionName}) @ '{Address}'")]
    internal static partial void ConnectionDisconnected(this ILogger logger, string externalConnectionId, string externalConnectionName, string address);

    internal static IDisposable TopicsSubscribe(this ILogger<MqttExternalIncomingCommunication> logger, string topics)
        => logger.CreateContext(_ => _.TopicsSubscribing(), _ => _.TopicsSubscribed(topics));

    [LoggerMessage(5, LogLevel.Debug, "Subscribing topics...")]
    internal static partial void TopicsSubscribing(this ILogger<MqttExternalIncomingCommunication> logger);

    [LoggerMessage(6, LogLevel.Debug, "Subscribed topics: {Topics}")]
    internal static partial void TopicsSubscribed(this ILogger<MqttExternalIncomingCommunication> logger, string topics);

    internal static IDisposable TopicsUnsubscribe(this ILogger<MqttExternalIncomingCommunication> logger)
        => logger.CreateContext(_ => _.TopicsUnsubscribing(), _ => _.TopicsUnsubscribed());

    [LoggerMessage(7, LogLevel.Debug, "Unsubscribing topics...")]
    internal static partial void TopicsUnsubscribing(this ILogger<MqttExternalIncomingCommunication> logger);

    [LoggerMessage(8, LogLevel.Debug, "Unsubscribed topics")]
    internal static partial void TopicsUnsubscribed(this ILogger<MqttExternalIncomingCommunication> logger);

    [LoggerMessage(9, LogLevel.Trace,
        "Received message from '{ExternalConnectionId}' ({ExternalConnectionName}) of client '{ClientId}' at topic '{Topic}'")]
    internal static partial void MessageReceived(this ILogger<MqttExternalIncomingCommunication> logger,
        string externalConnectionId, string externalConnectionName, string clientId, string topic);

    [LoggerMessage(10, LogLevel.Error,
        "Could not process message from '{ExternalConnectionId}' ({ExternalConnectionName}) of client '{ClientId}' at topic '{Topic}'")]
    internal static partial void MessageNotProcessable(this ILogger<MqttExternalIncomingCommunication> logger, Exception ex,
        string externalConnectionId, string externalConnectionName, string clientId, string topic);

    [LoggerMessage(11, LogLevel.Trace, "Send message to '{ExternalConnectionId}' ({ExternalConnectionName}) at topic '{Topic}'")]
    internal static partial void MessageSend(this ILogger<MqttExternalOutgoingCommunication> logger,
        string externalConnectionId, string externalConnectionName, string topic);

    [LoggerMessage(12, LogLevel.Error, "Could not send message to '{ExternalConnectionId}' ({ExternalConnectionName}) at topic '{Topic}'")]
    internal static partial void MessageNotSend(this ILogger<MqttExternalOutgoingCommunication> logger, Exception ex,
        string externalConnectionId, string externalConnectionName, string topic);
}
