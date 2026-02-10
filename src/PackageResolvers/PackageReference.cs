namespace ViciOne.ManagedEngine.PackageResolver;

/// <summary>
/// Represents a reference to a NuGet package.
/// </summary>
public class PackageReference
{
    /// <summary>
    /// Gets or sets the package name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    public string Version { get; set; } = string.Empty;
}
