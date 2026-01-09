using System.Collections.Generic;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal class GetDeploymentStates : IRequest<IReadOnlyDictionary<string, EngineState>>
{ }
