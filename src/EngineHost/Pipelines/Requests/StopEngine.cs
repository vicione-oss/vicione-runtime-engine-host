namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal class StopEngine : IRequest<bool>
{
    public string DeploymentIdentifier { get; set; } = string.Empty;
}
