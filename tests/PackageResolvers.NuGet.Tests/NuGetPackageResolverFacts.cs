using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.ManagedEngine.PackageResolver;

public sealed class NuGetPackageResolverFacts_ResolveAsync : IPackageResolverFacts.IResolveAsync
{
    [Fact]
    public async Task Returns_content_of_packages_Async()
    {
        var resolver = new NuGetPackageResolver();

        var (packages, _) = await resolver.ResolveAsync(
        [
            new() { Name = "Newtonsoft.Json", Version = "13.0.3", },
            new() { Name = "Microsoft.Extensions.DependencyInjection.Abstractions", Version = "8.0.1", }
        ], CancellationToken.None);

        packages.Should().HaveCount(2).And.SatisfyRespectively(
            dependencyModel =>
            {
                var asset = dependencyModel.Assets.Should().ContainSingle().Which;
                asset.Filename.Split(Path.DirectorySeparatorChar).Should().ContainInOrder("microsoft.extensions.dependencyinjection.abstractions", "8.0.1", "lib", "net6.0", "Microsoft.Extensions.DependencyInjection.Abstractions.dll");
            },
            newtonsoft =>
            {
                var asset = newtonsoft.Assets.Should().ContainSingle().Which;
                asset.Filename.Split(Path.DirectorySeparatorChar).Should().ContainInOrder("newtonsoft.json", "13.0.3", "lib", "net6.0", "Newtonsoft.Json.dll");
            });
    }

    [Fact]
    public async Task Returns_load_info_of_packages_Async()
    {
        var resolver = new NuGetPackageResolver();

        var (_, loadInfo) = await resolver.ResolveAsync(
        [
            new() { Name = "Newtonsoft.Json", Version = "13.0.3", },
            new() { Name = "Microsoft.Extensions.DependencyModel", Version = "8.0.0", }
        ], CancellationToken.None);

        loadInfo.DependencyFileContent.Should().ContainAll("Newtonsoft.Json", "13.0.3").And.ContainAll("Microsoft.Extensions.DependencyModel", "8.0.0");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_if_name_is_not_available_Async()
    {
        var resolver = new NuGetPackageResolver();

        var act = FluentActions.Invoking(async () => await resolver.ResolveAsync(
        [
            new() { Name = "unknown", Version = "1.0.0", },
        ], CancellationToken.None));

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*resolve*package*").WithInnerException<AggregateException>().WithMessage("*unknown*");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_if_version_is_not_available_Async()
    {
        var resolver = new NuGetPackageResolver();

        var act = FluentActions.Invoking(async () => await resolver.ResolveAsync(
        [
            new() { Name = "Newtonsoft.Json", Version = "0.1.0", },
        ], CancellationToken.None));

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*resolve*package*").WithInnerException<AggregateException>().WithMessage("*Newtonsoft.Json*0.1.0*");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_on_empty_name_Async()
    {
        var resolver = new NuGetPackageResolver();

        var act = FluentActions.Invoking(async () => await resolver.ResolveAsync(
        [
            new() { Name = string.Empty, Version = "0.1.0", },
        ], CancellationToken.None));

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*resolve*package*").WithInnerException<ArgumentException>().WithParameterName("term").WithMessage("*not*empty*");
    }

    [Fact]
    public async Task Throws_InvalidOperationException_on_empty_version_Async()
    {
        var resolver = new NuGetPackageResolver();

        var act = FluentActions.Invoking(async () => await resolver.ResolveAsync(
        [
            new() { Name = "Newtonsoft.Json", Version = string.Empty, },
        ], CancellationToken.None));

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*resolve*package*").WithInnerException<ArgumentException>().WithParameterName("value").WithMessage("*not*empty*");
    }

    [Fact(Skip = "Version conflict detection can not be tested. But it is tested by the creator.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped", Justification = "Nicht testbar")]
    public Task Throws_InvalidOperationException_on_version_conflict_Async() => Task.CompletedTask;
}
