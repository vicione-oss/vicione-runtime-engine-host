using System;
using System.Security.Authentication;

namespace ViciOne.ManagedEngine.Communication;

[Communication("07AB9DE8-6216-41FE-9E94-D324C5C55F7A")]
public class MqttCommunication : ICommunication
{
    public string? Host { get; set; }
    public int? Port { get; set; }
    public Uri? Uri { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? CertificateFile { get; set; }
    public string? CertificateFilePassword { get; set; }
    public string? CertificatePrivateKeyFile { get; set; }
    public SslProtocols? SslProtocol { get; set; }
    public bool? DisableCertificateValidation { get; set; }
    public short QualityOfService { get; set; }
    public bool? CleanSession { get; set; }
    public uint? SessionExpiryInterval { get; set; }
    public int? MaxPendingMessages { get; set; }
    public uint? MessageExpiryInterval { get; set; }
    public bool? Retain { get; set; }
}
