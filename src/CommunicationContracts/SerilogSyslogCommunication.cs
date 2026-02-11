namespace ViciOne.ManagedEngine.Communication;

/// <summary>
/// Communication configuration for Serilog syslog logging.
/// </summary>
[Communication("6B922B53-2849-44AC-87D4-898CBB09ADDC")]
public class SerilogSyslogCommunication : ICommunication
{
    /// <summary>
    /// Gets or sets the application name to include in syslog messages.
    /// </summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output template for formatting log messages.
    /// </summary>
    public string? OutputTemplate { get; set; }
}
