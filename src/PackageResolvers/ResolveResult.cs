using System.Collections.Generic;

namespace ViciOne.ManagedEngine.PackageResolver;

/// <summary>
/// Represents the result of a package resolution operation.
/// </summary>
/// <param name="Packages">The list of resolved packages.</param>
/// <param name="LoadInfo">The load information for the resolved packages.</param>
public record ResolveResult(List<Package> Packages, LoadInfo LoadInfo);

/// <summary>
/// Represents a resolved package with its assets.
/// </summary>
/// <param name="Directory">The directory containing the package.</param>
/// <param name="Assets">The list of assets in the package.</param>
public record Package(string Directory, List<Asset> Assets);

/// <summary>
/// Represents an asset file within a package.
/// </summary>
/// <param name="Filename">The filename of the asset.</param>
public record Asset(string Filename);

/// <summary>
/// Contains information needed to load resolved packages.
/// </summary>
/// <param name="DependencyFileContent">The content of the dependency file.</param>
public record LoadInfo(string DependencyFileContent);

/// <summary>
/// Represents a dependency file asset.
/// </summary>
public record DependencyFile : Asset
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyFile"/> class.
    /// </summary>
    /// <param name="filename">The filename of the dependency file.</param>
    public DependencyFile(string filename) : base(filename) { }
}
