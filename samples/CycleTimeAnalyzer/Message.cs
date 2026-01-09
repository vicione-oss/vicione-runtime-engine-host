using System.Diagnostics.CodeAnalysis;

namespace CycleTimeAnalyzer;

/// <summary>
/// Contract from engine host
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Für JSON Deserialisierung notwendig")]
public sealed record class Message(double CycleDuration, IEnumerable<DeploymentDuration> Deployments);
