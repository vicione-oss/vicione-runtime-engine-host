namespace ViciOne.ManagedEngine;

internal sealed record class CommandLineParameters
{
    public string DeployParameter { get; set; } = string.Empty;
    public string EngineHostUniqueIdentifier { get; set; } = string.Empty;
    public string MqttHost { get; set; } = string.Empty;
    public int MqttPort { get; set; }
    public bool MqttBroker { get; set; }
}
