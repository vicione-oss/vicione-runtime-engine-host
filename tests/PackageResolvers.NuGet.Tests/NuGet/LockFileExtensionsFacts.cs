using AwesomeAssertions;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using NuGet.Versioning;
using Xunit;

namespace ViciOne.ManagedEngine.PackageResolver.NuGet;

public sealed class LockFileExtensionsFacts_ToDependencyContext
{
    [Fact]
    public void Can_convert()
    {
        var lockFile = new LockFile()
        {
            Libraries =
            [
                new()
                {
                    Name = "Numeric.Add",
                    Version = NuGetVersion.Parse("1.0.0"),
                    Type = "package",
                    Path = "numeric.add/1.0.0",
                },
                new()
                {
                    Name = "Calculation",
                    Version = NuGetVersion.Parse("1.0.0-rc1"),
                    Type = "package",
                    Path = "calculation/1.0.0-rc1",
                },
            ],
            Targets =
            [
                new()
                {
                    TargetFramework = FrameworkConstants.CommonFrameworks.Net60,
                    Libraries =
                    [
                        new()
                        {
                            Name = "Numeric.Add",
                            Version = NuGetVersion.Parse("1.0.0"),
                            Type = "package",
                            RuntimeAssemblies =
                            [
                                new("lib/net6/Numeric.Add.dll"),
                            ],
                            Dependencies =
                            [
                                new("Calculation", new(minVersion: NuGetVersion.Parse("1.0.0-rc1"))),
                            ],
                        },
                        new()
                        {
                            Name = "Calculation",
                            Version = NuGetVersion.Parse("1.0.0-rc1"),
                            Type = "package",
                            RuntimeAssemblies =
                            [
                                new("runtimes/win/calc.dll"),
                                new("runtimes/arm/calc.dll"),
                            ],
                        },
                    ],
                },
            ],
        };

        var context = lockFile.ToDependencyContext();

        context.RuntimeLibraries.Should().HaveCount(2).And.SatisfyRespectively(
            numericAdd =>
            {
                numericAdd.Name.Should().Be("Numeric.Add");
                numericAdd.Version.ToString().Should().Be("1.0.0");
                numericAdd.RuntimeAssemblyGroups.Should().ContainSingle().Which.AssetPaths.Should().ContainSingle().Which.Should().Be("lib/net6/Numeric.Add.dll");
                var dependency = numericAdd.Dependencies.Should().ContainSingle().Which;
                dependency.Name.Should().Be("Calculation");
                dependency.Version.ToString().Should().Be("1.0.0-rc1");
            },
            calculation =>
            {
                calculation.Name.Should().Be("Calculation");
                calculation.Version.ToString().Should().Be("1.0.0-rc1");
                calculation.RuntimeAssemblyGroups.Should().ContainSingle().Which.AssetPaths.Should().HaveCount(2).And.Contain("runtimes/win/calc.dll").And.Contain("runtimes/arm/calc.dll");
            });
    }
}
