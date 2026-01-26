using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AwesomeAssertions;
using Testably.Abstractions.Testing;
using ViciOne.ManagedEngine.PackageResolver;
using Xunit;

namespace ViciOne.ManagedEngine.EngineDeployment;

public class PackageInstaller_Resolve
{
    [Fact]
    public void Returns_different_files()
    {
        var fileSystem = new MockFileSystem();

        Package packageA = new(Path.Combine("dir", "A"),
        [
            new(Path.Combine("dir", "A", "Assembly.dll")),
        ]);
        Package packageB = new(Path.Combine("dir", "B"),
        [
            new(Path.Combine("dir", "B", "runtime", "native", "NativeAssembly.so")),
        ]);

        List<Package> packages =
        [
            packageA,
            packageB,
        ];

        var files = PackageInstaller.Resolve(packages, fileSystem, CancellationToken.None);

        files.Should().HaveCount(2).And.SatisfyRespectively(
            e =>
            {
                e.Filename.Should().Be("Assembly");
                e.Package.Should().Be(packageA);
            },
            e =>
            {
                e.Filename.Should().Be("NativeAssembly");
                e.Package.Should().Be(packageB);
            });
    }

    [Fact]
    public void Ignores_the_file_hierarchy()
    {
        DirectoryMock directory = new();
        Package packageA = new(directory.CreateDirectory("A", "0.1.0"),
        [
            new(Path.Combine(directory.Path, "A", "0.1.0", "Assembly.dll")),
        ]);
        Package packageB = new(directory.CreateDirectory("B", "0.1.0"),
        [
            new(Path.Combine(directory.CreateDirectory(directory.Path, "B", "0.1.0", "runtime", "win-x64"), "Assembly.dll")),
        ]);
        List<Package> packages =
        [
            packageA,
            packageB,
        ];

        AssemblyHelper.CreateAssembly(packageA.Assets[0].Filename, "Assembly", "0.1.0", directory.FileSystem);
        AssemblyHelper.CreateAssembly(packageB.Assets[0].Filename, "Assembly", "0.1.0", directory.FileSystem);

        var files = PackageInstaller.Resolve(packages, directory.FileSystem, CancellationToken.None);

        var file = files.Should().ContainSingle().Which;
        file.Filename.Should().Be("Assembly");
        file.Package.Should().Be(packageA);
    }

    [Fact]
    public void Aggregates_by_filename_without_extension()
    {
        var fileSystem = new MockFileSystem();

        Package packageA = new(Path.Combine("dir", "A"),
        [
            new(Path.Combine("dir", "A", "Assembly.dll")),
            new(Path.Combine("dir", "A", "Assembly.pdb")),
        ]);
        Package packageB = new(Path.Combine("dir", "B"),
        [
            new(Path.Combine("dir", "B", "Assembly.dll")),
            new(Path.Combine("dir", "B", "Assembly.pdb")),
        ]);

        List<Package> packages =
        [
            packageA,
            packageB,
        ];

        var files = PackageInstaller.Resolve(packages, fileSystem, CancellationToken.None);

        files.Should().ContainSingle().And.SatisfyRespectively(
            e =>
            {
                e.Filename.Should().Be("Assembly");
                e.Package.Should().Be(packageB);
            });
    }
}

public class PackageInstaller_ByHighestVersionOrLastFile
{
    private readonly Package _package = new(string.Empty, []);

    [Fact]
    public void Returns_last_file_if_no_version_is_available()
    {
        var fileSystem = new MockFileSystem();
        PackageInstaller.AssetFileCandidate[] assets =
        [
            new("file", default, _package),
            new("file", default, _package),
            new("file", default, _package),
        ];

        var result = assets.Aggregate((file1, file2) => PackageInstaller.ByHighestVersionOrLastFile(file1, file2, fileSystem));

        result.Should().BeSameAs(assets[2]);
    }

    [Fact]
    public void Returns_file_with_highest_version()
    {
        var fileSystem = new MockFileSystem();
        PackageInstaller.AssetFileCandidate[] assets =
        [
            new("A", new("1.0.0"), _package),
            new("B", new("1.1.1"), _package),
            new("C", new("1.1.0"), _package),
        ];

        var result = assets.Aggregate((file1, file2) => PackageInstaller.ByHighestVersionOrLastFile(file1, file2, fileSystem));

        result.Should().BeSameAs(assets[1]);
    }

    [Fact]
    public void Returns_first_file_of_equal_versioned_assemblies()
    {
        var fileSystem = new MockFileSystem();
        PackageInstaller.AssetFileCandidate[] assets =
        [
            new("A", new("1.1.1"), _package),
            new("B", new("1.1.1"), _package),
        ];

        var result = assets.Aggregate((file1, file2) => PackageInstaller.ByHighestVersionOrLastFile(file1, file2, fileSystem));

        result.Should().BeSameAs(assets[0]);
    }

    [Fact]
    public void Returns_file_by_highest_version_in_name()
    {
        var fileSystem = new MockFileSystem();
        PackageInstaller.AssetFileCandidate[] assets =
        [
            new("file.so.3", default, _package),
            new("file.so.6", default, _package),
            new("file.so.1", default, _package),
        ];

        var result = assets.Aggregate((file1, file2) => PackageInstaller.ByHighestVersionOrLastFile(file1, file2, fileSystem));

        result.Should().BeSameAs(assets[1]);
    }

    [Fact]
    public void Returns_file_descending()
    {
        var fileSystem = new MockFileSystem();
        PackageInstaller.AssetFileCandidate[] assets =
        [
            new("file.dll", default, _package),
            new("file.so", default, _package),
            new("file.dylib", default, _package),
        ];

        var result = assets.Aggregate((file1, file2) => PackageInstaller.ByHighestVersionOrLastFile(file1, file2, fileSystem));

        result.Should().BeSameAs(assets[1]);
    }
}
