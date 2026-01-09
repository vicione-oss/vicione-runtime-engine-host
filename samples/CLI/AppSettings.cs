using ViciOne.ManagedEngine.EngineDeployment;

namespace ViciOne.ManagedEngine;

internal sealed record class AppSettings(
    DeployParameter[] DeployParameters,
    string EngineHostUniqueIdentifier,
    string MqttHost,
    int MqttPort,
    bool MqttBroker);
