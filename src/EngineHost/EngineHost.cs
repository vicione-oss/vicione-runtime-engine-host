using System;
using System.Reflection;

namespace ViciOne.ManagedEngine;

internal static class EngineHost
{
    private static readonly string? s_version = GetAssemblyInformationalVersion();
    private static readonly string? s_versionWithoutMetadata = VersionWithoutMetadata(GetAssemblyInformationalVersion());

    internal static string InformationalVersion => s_version ?? throw new InvalidOperationException("Cannot get host version.");
    internal static string Version => s_versionWithoutMetadata ?? throw new InvalidOperationException("Cannot get host version.");

    internal static string? VersionWithoutMetadata(string? version)
    {
        if (version is null)
            return null;

        var indexOfMetadataStart = version.IndexOf('+', StringComparison.InvariantCultureIgnoreCase);

        if (indexOfMetadataStart == -1)
            return version;

        return version[..indexOfMetadataStart];
    }

    private static string? GetAssemblyInformationalVersion()
        => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
