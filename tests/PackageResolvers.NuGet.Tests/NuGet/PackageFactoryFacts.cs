using System.IO;
using System.Linq;
using AwesomeAssertions;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using Xunit;

namespace ViciOne.ManagedEngine.PackageResolver.NuGet;

public sealed class PackageFactoryFacts_Create
{
    [Fact]
    public void Returns_packages()
    {
        var lockFile = new LockFile()
        {
            PackageFolders =
            [
                new("cache"),
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
                            Name = "vicione.engine",
                            Version = new(1, 0, 0),
                            Type = "package",
                            RuntimeAssemblies =
                            [
                                new("lib/net6/ViciOne.Engine.dll"),
                            ],
                            RuntimeTargets =
                            [
                                new("runtimes/win-x64/native/ViciOne.Driver.dll", "win-x64", "native"),
                            ],
                            ContentFiles =
                            [
                                new("contentFiles/any/net6/fbs/Numeric.Add.dll") { CopyToOutput = true, },
                                new("contentFiles/cs/any/Scripts.cs") { CopyToOutput = true, },
                            ],
                        },
                        new()
                        {
                            Name = "vicione.ui",
                            Version = new(2, 1, 0),
                            Type = "package",
                            RuntimeAssemblies =
                            [
                                new("lib/net6/ViciOne.Ui.dll"),
                            ],
                            RuntimeTargets =
                            [
                                new("runtimes/win-x64/lib/net6/VicOne.Resources.Win.dll", "win-x64", "runtime"),
                            ],
                            ResourceAssemblies =
                            [
                                new("lib/net6/VicOne.Resources.dll"),
                                new("lib/net6/de/VicOne.Exceptions.dll"),
                                new("lib/net6/en/VicOne.Exceptions.dll"),
                            ],
                        },
                        new()
                        {
                            Name = "vicione.nothing",
                            Version = new(1, 0,0),
                            Type  = "no package",
                            RuntimeAssemblies =
                            [
                                new("lib/net6/ViciOne.Nothing.dll"),
                            ],
                        },
                    ],
                },
            ],
        };

        var packages = PackageFactory.Create(lockFile, FrameworkConstants.CommonFrameworks.Net60, l => Path.Combine("cache", l.Name?.ToUpperInvariant() ?? string.Empty, l.Version?.ToNormalizedString().ToUpperInvariant() ?? string.Empty)).ToList();

        packages.Should().HaveCount(2).And.SatisfyRespectively(
            engine => engine.Assets.Should().BeEquivalentTo(new Asset[]
            {
                new(Path.Combine("cache", "VICIONE.ENGINE", "1.0.0", "lib", "net6", "ViciOne.Engine.dll")),
                new(Path.Combine("cache", "VICIONE.ENGINE", "1.0.0", "runtimes", "win-x64", "native", "ViciOne.Driver.dll")),
                new(Path.Combine("cache", "VICIONE.ENGINE", "1.0.0", "contentFiles", "any", "net6", "fbs", "Numeric.Add.dll")),
                new(Path.Combine("cache", "VICIONE.ENGINE", "1.0.0", "contentFiles", "cs", "any", "Scripts.cs")),
            }),
            ui => ui.Assets.Should().BeEquivalentTo(new Asset[]
            {
                new(Path.Combine("cache", "VICIONE.UI", "2.1.0", "lib", "net6", "ViciOne.Ui.dll")),
                new(Path.Combine("cache", "VICIONE.UI", "2.1.0", "runtimes", "win-x64", "lib", "net6", "VicOne.Resources.Win.dll")),
                new(Path.Combine("cache", "VICIONE.UI", "2.1.0", "lib", "net6", "VicOne.Resources.dll")),
                new(Path.Combine("cache", "VICIONE.UI", "2.1.0", "lib", "net6", "de", "VicOne.Exceptions.dll")),
                new(Path.Combine("cache", "VICIONE.UI", "2.1.0", "lib", "net6", "en", "VicOne.Exceptions.dll")),
            }));
    }
}
