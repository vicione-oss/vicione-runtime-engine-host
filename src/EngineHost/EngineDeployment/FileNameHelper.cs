using System.IO.Abstractions;

namespace ViciOne.ManagedEngine.EngineDeployment;

internal static class FileNameHelper
{
    private const string StartParameter = "start-parameter.json.gz";
    private const string DeployParameter = "deploy-parameter.json.gz";

    internal static string GetStartParameterFileName(string directory, IFileSystem fileSystem) => fileSystem.Path.Combine(directory, StartParameter);
    internal static string GetDeployParameterFileName(string directory, IFileSystem fileSystem) => fileSystem.Path.Combine(directory, DeployParameter);
}
