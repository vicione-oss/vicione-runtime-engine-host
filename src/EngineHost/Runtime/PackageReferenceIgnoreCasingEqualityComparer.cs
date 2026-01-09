using System;
using System.Collections.Generic;
using ViciOne.ManagedEngine.PackageResolver;

namespace ViciOne.ManagedEngine.Runtime;

internal sealed class PackageReferenceIgnoreCasingEqualityComparer : IEqualityComparer<PackageReference>
{
    internal static PackageReferenceIgnoreCasingEqualityComparer Default { get; } = new();

    public bool Equals(PackageReference? x, PackageReference? y)
    {
        if (x is null && y is null)
            return true;
        if (x is null || y is null)
            return false;

        return x.Name.Equals(y.Name, StringComparison.OrdinalIgnoreCase) &&
            x.Version.Equals(y.Version, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(PackageReference obj)
    {
        HashCode hashCode = new();

        hashCode.Add(obj.Name, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(obj.Version, StringComparer.OrdinalIgnoreCase);

        return hashCode.ToHashCode();
    }
}
