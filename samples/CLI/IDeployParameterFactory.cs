using ViciOne.ManagedEngine.EngineDeployment;

namespace ViciOne.ManagedEngine;

internal interface IDeployParameterFactory
{
    DeployParameter[] CreateDeployParameters();
}
