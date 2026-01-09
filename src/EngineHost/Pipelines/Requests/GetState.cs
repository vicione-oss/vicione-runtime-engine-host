namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal class GetState : IRequest<EngineState>
{
    public string DeploymentIdentifier { get; set; } = string.Empty;
}
