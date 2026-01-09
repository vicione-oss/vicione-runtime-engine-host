namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal class TearDownEngine : IRequest<bool>
{
    public string DeploymentIdentifier { get; set; } = string.Empty;
}
