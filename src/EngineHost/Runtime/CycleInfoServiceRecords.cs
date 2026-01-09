using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ViciOne.ManagedEngine.Runtime;

internal record InfoMessage(double CycleDuration, IEnumerable<DeploymentDuration> Deployments);

internal record DeploymentDuration
{
    public DeploymentDuration(string deployment, double duration, ulong? cycle)
    {
        Deployment = deployment;
        Duration = duration;
        Cycle = cycle;
    }

    public string Deployment { get; init; }

    public double Duration { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Cycle { get; init; }
}

internal record CrashMessage(IEnumerable<string> Deployments);
