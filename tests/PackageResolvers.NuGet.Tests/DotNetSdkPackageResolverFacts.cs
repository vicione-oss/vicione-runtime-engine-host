using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.ManagedEngine.PackageResolver;

public sealed class DotNetSdkPackageResolverFacts_ResolveAsync : IPackageResolverFacts.IResolveAsync
{
    [Fact]
    public async Task Returns_content_of_packages_Async()
    {
        using TemporaryDirectory directory = new();
        var resolver = new DotNetSdkPackageResolver(directory.Path, directory.FileSystem);

        var (packages, _) = await resolver.ResolveAsync(
        [
            new() { Name = "Newtonsoft.Json", Version = "13.0.1", },
            new() { Name = "Microsoft.Extensions.DependencyModel", Version = "5.0.0", }
        ], CancellationToken.None);

        var assets = packages.Should().ContainSingle().Subject.Assets;
        assets.Select(a => directory.FileSystem.Path.GetFileName(a.Filename)).Should().BeEquivalentTo(
            "meta.dll", "meta.deps.json", "meta.pdb", "Microsoft.Extensions.DependencyModel.dll", "Newtonsoft.Json.dll");
    }

    [Fact]
    public async Task Returns_load_info_of_packages_Async()
    {
        using TemporaryDirectory directory = new();
        var resolver = new DotNetSdkPackageResolver(directory.Path, directory.FileSystem);

        var (_, loadInfo) = await resolver.ResolveAsync(
        [
            new() { Name = "Newtonsoft.Json", Version = "13.0.1", },
            new() { Name = "Microsoft.Extensions.DependencyModel", Version = "5.0.0", }
        ], CancellationToken.None);

        loadInfo.DependencyFileContent.Should().ContainAll("Newtonsoft.Json", "13.0.1").And.ContainAll("Microsoft.Extensions.DependencyModel", "5.0.0");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_if_name_is_not_available_Async()
    {
        using TemporaryDirectory directory = new();
        var resolver = new DotNetSdkPackageResolver(directory.Path, directory.FileSystem);

        var act = FluentActions.Invoking(async () => await resolver.ResolveAsync(
        [
            new() { Name = "unknown", Version = "1.0.0", },
        ], CancellationToken.None));

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*resolve*package*")
            .WithInnerException<InvalidOperationException>().WithMessage("*unable*find*unknown*");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_if_version_is_not_available_Async()
    {
        using TemporaryDirectory directory = new();
        var resolver = new DotNetSdkPackageResolver(directory.Path, directory.FileSystem);

        var act = FluentActions.Invoking(async () => await resolver.ResolveAsync(
        [
            new() { Name = "Newtonsoft.Json", Version = "0.1.0", },
        ], CancellationToken.None));

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*resolve*package*")
            .WithInnerException<InvalidOperationException>().WithMessage("*Newtonsoft.Json*0.1.0*not found*");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_on_empty_name_Async()
    {
        using TemporaryDirectory directory = new();
        var resolver = new DotNetSdkPackageResolver(directory.Path, directory.FileSystem);

        var act = FluentActions.Invoking(async () => await resolver.ResolveAsync(
        [
            new() { Name = string.Empty, Version = "0.1.0", },
        ], CancellationToken.None));

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*resolve*package*")
            .WithInnerException<InvalidOperationException>().WithMessage("*Include*empty*element*PackageReference*");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_on_empty_version_Async()
    {
        using TemporaryDirectory directory = new();
        var resolver = new DotNetSdkPackageResolver(directory.Path, directory.FileSystem);

        var act = FluentActions.Invoking(async () => await resolver.ResolveAsync(
        [
            new() { Name = "Newtonsoft.Json", Version = string.Empty, },
        ], CancellationToken.None));

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*resolve*package*")
            .WithInnerException<InvalidOperationException>().WithMessage("*no*version*specified*Newtonsoft.Json*");
    }

    [Fact(Skip = "Version conflict detection can not be tested. But it is tested by the creators of .NET.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped", Justification = "Nicht testbar")]
    public Task Throws_InvalidOperationException_on_version_conflict_Async() => Task.CompletedTask;
}
