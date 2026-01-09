using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using ViciOne.ManagedEngine.PackageResolver;
using Xunit;

namespace ViciOne.ManagedEngine.TypeResolution;

public class PackagesAssemblyLoadContextFactory_ToMainComponents
{
    [Fact]
    public void Returns_main_components()
    {
        List<(string, Package)> files =
        [
            (string.Empty, new Package(string.Empty,
            [
                new DependencyFile(Path.Combine("dir", "A", "Assembly.deps.json")),
                new DependencyFile(Path.Combine("dir", "A", "False.deps.json")),
            ])),
            (string.Empty, new Package(string.Empty,
            [
                new DependencyFile(Path.Combine("dir", "B", "Assembly.deps.json")),
            ])),
        ];

        var components = PackagesAssemblyLoadContextFactory.ToMainComponents(files).ToArray();

        components.Should().SatisfyRespectively(
            e => e.PackageMainComponent.Should().Be(Path.Combine("dir", "A", "Assembly.dll")),
            e => e.PackageMainComponent.Should().Be(Path.Combine("dir", "B", "Assembly.dll")));
    }
}
