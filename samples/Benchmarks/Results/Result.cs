namespace Benchmarks.Results;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. Used as a generic parameter and therefore instantiated.
internal sealed class Result
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    public string Device { get; set; } = string.Empty;
    public string Processor { get; set; } = string.Empty;
    public string Memory { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string BenchmarkDotNet { get; set; } = string.Empty;
    public string DotNetSdk { get; set; } = string.Empty;
    public string DotNet { get; set; } = string.Empty;
    public string CommunicationPartnerLocation { get; set; } = string.Empty;
    public string CommunicationPartner { get; set; } = string.Empty;
    public string ConnectionSpeed { get; set; } = string.Empty;
    public int TotalValues { get; set; }
    public int Links { get; set; }
    public string Technology { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Direction Direction { get; set; }
    public double Throughput { get; set; }
}
