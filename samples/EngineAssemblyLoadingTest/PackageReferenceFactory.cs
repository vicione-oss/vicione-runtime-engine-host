using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ManagedEngine.PackageResolver;

namespace ViciOne.EngineHost.EngineAssemblyLoadingTest;

internal static class PackageReferenceFactory
{
    internal static PackageReference Parse(string text)
        => ParseCore(text) ?? throw new FormatException("The package reference has an invalid format.");

    internal static bool TryParse(string text, [NotNullWhen(true)] out PackageReference? packageReference)
    {
        packageReference = ParseCore(text);
        return packageReference is not null;
    }

    private static PackageReference? ParseCore(string text)
    {
        PackageReference? packageReference = null;
        var parts = text.Split('@');
        if (parts.Length == 2)
            packageReference = new() { Name = parts[0], Version = parts[1], };
        return packageReference;
    }
}
