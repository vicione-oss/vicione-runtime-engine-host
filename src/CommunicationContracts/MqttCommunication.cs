using System;
using System.Security.Authentication;

namespace ViciOne.ManagedEngine.Communication;

/// <summary>
/// Communication configuration for MQTT-based messaging.
/// </summary>
[Communication("07AB9DE8-6216-41FE-9E94-D324C5C55F7A")]
public class MqttCommunication : ICommunication
{
    /// <summary>
    /// Gets or sets the MQTT broker TCP hostname.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the MQTT broker TCP port number.
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// Gets or sets the MQTT broker web socket URI.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// Gets or sets the username for MQTT broker authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for MQTT broker authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the path to the client certificate file.
    /// </summary>
    public string? CertificateFile { get; set; }

    /// <summary>
    /// Gets or sets the password for the client certificate file.
    /// </summary>
    public string? CertificateFilePassword { get; set; }

    /// <summary>
    /// Gets or sets the path to the client certificate private key file.
    /// </summary>
    public string? CertificatePrivateKeyFile { get; set; }

    /// <summary>
    /// Gets or sets the SSL/TLS protocol version to use.
    /// </summary>
    public SslProtocols? SslProtocol { get; set; }

    /// <summary>
    /// Gets or sets whether to disable server certificate validation.
    /// </summary>
    public bool? DisableCertificateValidation { get; set; }

    /// <summary>
    /// Gets or sets the MQTT Quality of Service level (0, 1, or 2).
    /// </summary>
    public short QualityOfService { get; set; }

    /// <summary>
    /// Gets or sets whether to start with a clean session.
    /// </summary>
    public bool? CleanSession { get; set; }

    /// <summary>
    /// Gets or sets the session expiry interval in seconds.
    /// </summary>
    public uint? SessionExpiryInterval { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of pending messages.
    /// </summary>
    public int? MaxPendingMessages { get; set; }

    /// <summary>
    /// Gets or sets the message expiry interval in seconds.
    /// </summary>
    public uint? MessageExpiryInterval { get; set; }

    /// <summary>
    /// Gets or sets whether messages should be retained by the broker.
    /// </summary>
    public bool? Retain { get; set; }
}
