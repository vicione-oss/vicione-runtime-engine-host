using System.Collections.Generic;

namespace ViciOne.ManagedEngine.PackageResolver;

public record ResolveResult(List<Package> Packages, LoadInfo LoadInfo);
public record Package(string Directory, List<Asset> Assets);
public record Asset(string Filename);
public record LoadInfo(string DependencyFileContent);

public record DependencyFile : Asset
{
    public DependencyFile(string filename) : base(filename) { }
}
