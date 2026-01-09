namespace ViciOne.ManagedEngine.Communication;

[Communication("6B922B53-2849-44AC-87D4-898CBB09ADDC")]
public class SerilogSyslogCommunication : ICommunication
{
    public string AppName { get; set; } = string.Empty;
    public string? OutputTemplate { get; set; }
}
