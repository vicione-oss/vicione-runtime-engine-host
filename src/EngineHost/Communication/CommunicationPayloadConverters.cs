using ViciOne.ManagedEngine.Pipelines.Requests;

namespace ViciOne.ManagedEngine.Communication;

internal static class CommunicationPayloadConverters
{
    internal static StartEngine ToStartEngine(string payload)
        => new() { DeploymentIdentifier = payload };

    internal static StopEngine ToStopEngine(string payload)
        => new() { DeploymentIdentifier = payload };

    internal static TearDownEngine ToTearDownEngine(string payload)
        => new() { DeploymentIdentifier = payload };

    internal static GetState ToGetState(string payload)
        => new() { DeploymentIdentifier = payload };

    internal static GetPackages ToGetPackages(string payload)
        => new() { DeploymentIdentifier = payload };

    internal static DeployEngine ToDeployEngine(DeployEnginePayload payload)
        => new()
        {
            StartParameter = payload.StartParameter.ToString(),
            PackageReferences = payload.PackageReferences,
            EngineChainIndex = payload.EngineChainIndex,
            CycleTime = payload.CycleTime,
        };
}
