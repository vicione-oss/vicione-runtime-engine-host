namespace ViciOne.ManagedEngine.Communication;

[Communication("E3601083-66AE-41FC-AC20-E05B1DD7936E")]
public class SeriLogFileCommunication : ICommunication
{
    public string Path { get; set; } = string.Empty;
    public string? OutputTemplate { get; set; }
}
