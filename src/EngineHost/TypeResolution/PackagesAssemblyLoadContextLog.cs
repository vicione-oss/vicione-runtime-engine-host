using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.TypeResolution;

internal static partial class PackagesAssemblyLoadContextLog
{
    [LoggerMessage(1, LogLevel.Debug, "{Context}: Load assembly '{Assembly}' from shared assemblies.{StackTrace}")]
    internal static partial void LoadAssemblyFromSharedAssemblies(this ILogger<PackagesAssemblyLoadContext> logger,
        string context, string assembly, string stackTrace);

    [LoggerMessage(2, LogLevel.Debug, "{Context}: Load assembly '{Assembly}' from file '{Path}'.{StackTrace}")]
    internal static partial void LoadAssemblyFromFile(this ILogger<PackagesAssemblyLoadContext> logger,
        string context, string assembly, string path, string stackTrace);

    [LoggerMessage(3, LogLevel.Debug, "{Context}: Load assembly '{Assembly}' from default context.{StackTrace}")]
    internal static partial void LoadAssemblyFromDefaultContext(this ILogger<PackagesAssemblyLoadContext> logger,
        string context, string assembly, string stackTrace);

    [LoggerMessage(4, LogLevel.Debug, "{Context}: Load native assembly '{Assembly}' from file '{Path}'.{StackTrace}")]
    internal static partial void LoadNativeAssemblyFromFile(this ILogger<PackagesAssemblyLoadContext> logger,
        string context, string assembly, string path, string stackTrace);

    [LoggerMessage(5, LogLevel.Debug, "{Context}: Load native assembly '{Assembly}' from default context.{StackTrace}")]
    internal static partial void LoadNativeAssemblyFromDefaultContext(this ILogger<PackagesAssemblyLoadContext> logger,
        string context, string assembly, string stackTrace);

    [LoggerMessage(6, LogLevel.Debug, "{Context}: Load assembly '{Assembly}' from cache.{StackTrace}")]
    internal static partial void LoadAssemblyFromCache(this ILogger<PackagesAssemblyLoadContext> logger,
        string context, string assembly, string stackTrace);

    [LoggerMessage(7, LogLevel.Debug, "{Context}: Load native assembly '{Assembly}' from cache.{StackTrace}")]
    internal static partial void LoadNativeAssemblyFromCache(this ILogger<PackagesAssemblyLoadContext> logger,
        string context, string assembly, string stackTrace);
}
