namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal class StartEngine : IRequest<bool>
{
    public string DeploymentIdentifier { get; set; } = string.Empty;
}
