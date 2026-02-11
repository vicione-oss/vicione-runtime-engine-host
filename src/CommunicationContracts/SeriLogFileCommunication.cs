namespace ViciOne.ManagedEngine.Communication;

/// <summary>
/// Communication configuration for Serilog file-based logging.
/// </summary>
[Communication("E3601083-66AE-41FC-AC20-E05B1DD7936E")]
public class SeriLogFileCommunication : ICommunication
{
    /// <summary>
    /// Gets or sets the file path for log output.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output template for formatting log messages.
    /// </summary>
    public string? OutputTemplate { get; set; }
}
