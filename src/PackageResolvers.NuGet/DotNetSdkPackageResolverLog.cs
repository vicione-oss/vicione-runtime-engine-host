using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.PackageResolver;

internal static partial class DotNetSdkPackageResolverLog
{
    [LoggerMessage(1, LogLevel.Trace, "Created directory '{Directory}' for temporary input files of 'publish' action")]
    internal static partial void DirectoryCreated(this ILogger logger, string directory);

    [LoggerMessage(2, LogLevel.Debug, "Created project with {PackageReferences} package reference(s) for 'publish' action")]
    internal static partial void ProjectCreated(this ILogger logger, int packageReferences);

    [LoggerMessage(3, LogLevel.Debug, "Created NuGet.Config with {Sources} custom source(s) for 'publish' action")]
    internal static partial void NuGetConfigCreated(this ILogger logger, int sources);

    [LoggerMessage(4, LogLevel.Debug, "Published temporary project for package resolution")]
    internal static partial void ProjectPublished(this ILogger logger);

    [LoggerMessage(10, LogLevel.Trace, "Removed directory for temporary input files of 'publish' action")]
    internal static partial void DirectoryRemoved(this ILogger logger);
}
