using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ViciOne.ManagedEngine;

internal static class DeployParameterFactory
{
    internal static IReadOnlyCollection<IDeployParameterFactory> FindAll()
    {
        var deployParameterFactoryApi = typeof(IDeployParameterFactory);
        List<IDeployParameterFactory> deployParameterFactories = [];
        foreach (var type in Assembly.GetCallingAssembly().DefinedTypes)
        {
            if (type.ImplementedInterfaces.Contains(deployParameterFactoryApi))
            {
                var instance = Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Deploy parameter factory '{type.Name}' could not be created."); ;
                deployParameterFactories.Add((IDeployParameterFactory)instance);
            }
        }
        return deployParameterFactories;
    }
}
