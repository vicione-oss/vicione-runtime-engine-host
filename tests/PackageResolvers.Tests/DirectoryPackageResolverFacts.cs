using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.ManagedEngine.PackageResolver;

public sealed class DirectoryPackageResolver_ResolveAsync : IPackageResolverFacts.IResolveAsync
{
    private readonly DirectoryMock _directory;
    private readonly DirectoryPackageResolver _packageResolver;

    public DirectoryPackageResolver_ResolveAsync()
    {
        _directory = new();
        _packageResolver = new DirectoryPackageResolver(_directory.Path, _directory.FileSystem);
    }

    [Fact]
    public async Task Returns_content_of_packages_Async()
    {
        _directory.CreateFile("A", "1.2.3-x.7.z.92", "A.dll");
        _directory.CreateFile("A", "1.2.3-x.7.z.92", "A.json");
        _directory.CreateFile("A", "1.2.3-x.7.z.92", "lib", "Interop.dll");
        _directory.CreateFile("B", "2.0.0-ci", "hello.dll");

        var (packages, _) = await _packageResolver.ResolveAsync(
        [
            new() { Name = "A", Version = "1.2.3-x.7.z.92" },
            new() { Name = "B", Version = "2.0.0-ci" },
        ], CancellationToken.None);

        packages.Should().HaveCount(2);

        packages.SelectMany(p => p.Assets)
            .Should()
                .HaveCount(4).And.Subject
            .Select(a => Path.GetFileName(a.Filename))
            .Should()
                .Contain("A.dll").And
                .Contain("A.json").And
                .Contain("Interop.dll").And
                .Contain("hello.dll");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_on_empty_name_Async()
    {
        _directory.CreateDirectory("A", "0.1.0");

        var act = FluentActions.Invoking(async () => await _packageResolver.ResolveAsync([new() { Name = string.Empty, Version = "0.1.0", }], CancellationToken.None));

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.WithMessage("*not*obtain*packages*").WithInnerException<AggregateException>().WithInnerException<ArgumentException>().WithParameterName("name").WithMessage("*not*null*");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_if_name_is_not_available_Async()
    {
        var act = FluentActions.Awaiting(() => _packageResolver.ResolveAsync([new() { Name = "A", Version = "1.2.0" }], CancellationToken.None));

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.WithMessage("*not*obtain*packages*").WithInnerException<AggregateException>().WithInnerException<InvalidOperationException>().WithMessage("*A*1.2.0*not*found*");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_on_empty_version_Async()
    {
        _directory.CreateDirectory("A", "0.1.0");

        var act = FluentActions.Invoking(async () => await _packageResolver.ResolveAsync([new() { Name = "A", Version = string.Empty, }], CancellationToken.None));

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.WithMessage("*not*obtain*packages*").WithInnerException<AggregateException>().WithInnerException<ArgumentException>().WithParameterName("version").WithMessage("*not*null*");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_if_version_is_not_available_Async()
    {
        _directory.CreateDirectory("A", "0.1.0");

        var act = FluentActions.Invoking(async () => await _packageResolver.ResolveAsync([new() { Name = "A", Version = "1.2.0" }], CancellationToken.None));

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.WithMessage("*not*obtain*packages*").WithInnerException<AggregateException>().WithInnerException<InvalidOperationException>().WithMessage("*A*1.2.0*not*found*");
    }

    [Fact(Skip = "Version conflict detection not supported by DirectoryPackageResolver")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped", Justification = "Nicht testbar")]
    public Task Throws_InvalidOperationException_on_version_conflict_Async() => Task.CompletedTask;

    [Fact]
    public async Task Can_handle_non_SemVer_named_directories_Async()
    {
        _directory.CreateDirectory("MyFb", "0.1.0");
        _directory.CreateDirectory("MyFb", "a735d96");
        _directory.CreateFile("MyFb", "0.2.0", "File.dll");

        var (packages, _) = await _packageResolver.ResolveAsync([new() { Name = "MyFb", Version = "0.2.0" }], CancellationToken.None);

        packages.Should().ContainSingle().Which.Assets.Select(a => Path.GetFileName(a.Filename)).Should().ContainSingle().Which.Should().Be("File.dll");
    }

    [Fact]
    public async Task Returns_load_info_of_packages_Async()
    {
        var file1 = _directory.CreateFile("FB1", "0.2.0", "MyFb.deps.json");
        var file2 = _directory.CreateFile("FB2", "2.2.0", "MyFb.deps.json");
        await _directory.FileSystem.File.WriteAllTextAsync(file1, @"
        {
            ""targets"": { 
                "".NETCoreApp, Version=v5.0"": {
                    ""ViciOne.EngineManagement.CLI.Sample/1.0.0"": { },
                    ""MediatR/9.0.0"": { }
                }
            },
            ""libraries"": {
                ""ViciOne.EngineManagement.CLI.Sample/1.0.0"": {
                    ""type"": ""project"",
                    ""serviceable"": false,
                    ""sha512"": """"
                },
                ""MediatR/9.0.0"": {
                        ""type"": ""package"",
                        ""serviceable"": true,
                        ""sha512"": ""sha512-8b3UYNxegHVYcJMG2zH8wn+YqxLvXG+eMfj0cMCq/jTW72p6O3PCKMkrIv0mqyxdW7bA4gblsocw7n+/9Akg5g=="",
                        ""path"": ""mediatr/9.0.0"",
                        ""hashPath"": ""mediatr.9.0.0.nupkg.sha512""
                }
            }
        }");
        await _directory.FileSystem.File.WriteAllTextAsync(file2, @"
        {
            ""targets"": {
                "".NETCoreApp, Version = v5.0"": {
                    ""System.Reflection/4.3.0"": { }
                }
            },
            ""libraries"": {
                ""System.Reflection/4.3.0"": {
                        ""type"": ""package"",
                        ""serviceable"": true,
                        ""sha512"": ""sha512-KMiAFoW7MfJGa9nDFNcfu+FpEdiHpWgTcS2HdMpDvt9saK3y/G4GwprPyzqjFH9NTaGPQeWNHU+iDlDILj96aQ=="",
                        ""path"": ""system.reflection/4.3.0"",
                        ""hashPath"": ""system.reflection.4.3.0.nupkg.sha512""
                }
            }
        }");

        var (_, loadInfo) = await _packageResolver.ResolveAsync([new() { Name = "FB1", Version = "0.2.0" }, new() { Name = "FB2", Version = "2.2.0" }], CancellationToken.None);

        loadInfo.DependencyFileContent.Should()
            .Contain("ViciOne.EngineManagement.CLI.Sample/1.0.0", Exactly.Twice()).And
            .Contain("MediatR/9.0.0", Exactly.Twice()).And
            .Contain("System.Reflection/4.3.0", Exactly.Twice());
    }
}
