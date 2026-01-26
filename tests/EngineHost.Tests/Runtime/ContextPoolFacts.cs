using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using ViciOne.ManagedEngine.PackageResolver;
using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

public sealed class ContextPool_RegisterDeployment : IDisposable
{
    private readonly ContextPoolContext _context = new();
    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Returns_new_context_if_references_are_not_equal_Async()
    {
        const string ExistingContextId = "existing context";

        AssemblyLoadContext existingContext = new(ExistingContextId, true);
        _context.ContextPool._contexts.Add(ExistingContextId, new() { Context = existingContext, ConsumerCount = 1, PackageReferences = [new(),], });

        var context = await _context.ContextPool.RegisterDeploymentAsync("deployment", [], CancellationToken.None);

        var id = context.Name.Should().NotBeNull().And.Subject;
        id.Should().NotBeNull().And.NotBe(ExistingContextId);
        context.Should().NotBe(existingContext);
        _context.ContextPool._contexts.Should().HaveCount(2);
        _context.ContextPool._contexts[id].ConsumerCount.Should().Be(1);
        _context.ContextPool._deploymentContextMap.Should().ContainSingle();
        _context.Logger.Calls.Should().Be(1);
        _context.Logger.LogLevel.Should().Be(LogLevel.Information);
        _context.Logger.EventId.Should().Be(new EventId(1, nameof(ContextPoolLog.CreatedContext)));
        _context.Logger.Exception.Should().BeNull();
        _context.Logger.Message.Should().MatchEquivalentOf($"*created*context*id*{context.Name}*deployment*deployment*");
    }

    [Fact]
    public async Task Returns_same_context_if_references_are_equal_Async()
    {
        var existingContextId = ContextPool.GetUniqueHash([]);

        AssemblyLoadContext existingContext = new(existingContextId, true);
        _context.ContextPool._contexts.Add(existingContextId, new() { Context = existingContext, ConsumerCount = 1, });

        var context = await _context.ContextPool.RegisterDeploymentAsync("deployment", [], CancellationToken.None);

        var id = context.Name.Should().NotBeNull().And.Subject;
        id.Should().Be(existingContextId);
        context.Should().NotBeNull().And.Be(existingContext);
        var contextInfo = _context.ContextPool._contexts.Should().ContainSingle().Which;
        contextInfo.Value.ConsumerCount.Should().Be(2);
        _context.ContextPool._deploymentContextMap.Should().ContainSingle();
        _context.Logger.Calls.Should().Be(1);
        _context.Logger.LogLevel.Should().Be(LogLevel.Debug);
        _context.Logger.EventId.Should().Be(new EventId(2, nameof(ContextPoolLog.UsedExistingContext)));
        _context.Logger.Exception.Should().BeNull();
        _context.Logger.Message.Should().MatchEquivalentOf($"*use*existing*id*{context.Name}*deployment*deployment*");
    }

    [Fact]
    public async Task Returns_collectible_context_Async()
    {
        var context = await _context.ContextPool.RegisterDeploymentAsync("deployment", [], CancellationToken.None);

        context.IsCollectible.Should().BeTrue();
    }
}

public sealed class ContextPool_UnregisterDeployment : IDisposable
{
    private readonly ContextPoolContext _context = new();
    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Keeps_context_if_consumers_exists_Async()
    {
        AssemblyLoadContext context = new("context", true);
        var unloaded = false;
        context.Unloading += _ => unloaded = true;
        _context.ContextPool._deploymentContextMap.Add("1", "context");
        _context.ContextPool._contexts.Add("context", new() { Context = context, ConsumerCount = 2, });

        await _context.ContextPool.UnregisterDeploymentAsync("1");

        unloaded.Should().BeFalse();
        var contextInfo = _context.ContextPool._contexts.Should().ContainSingle().Which;
        contextInfo.Value.ConsumerCount.Should().Be(1);
        _context.Logger.Calls.Should().Be(1);
        _context.Logger.LogLevel.Should().Be(LogLevel.Debug);
        _context.Logger.EventId.Should().Be(new EventId(8, nameof(ContextPoolLog.RemoveConsumer)));
        _context.Logger.Exception.Should().BeNull();
        _context.Logger.Message.Should().MatchEquivalentOf($"*remove*deployment*1*from*context*{context.Name}*");
    }

    [Fact]
    public async Task Unloads_context_if_no_more_consumers_exists_Async()
    {
        _context.Directory.CreateDirectory("context");
        AssemblyLoadContext context = new("context", true);
        var unloaded = false;
        context.Unloading += _ => unloaded = true;
        _context.ContextPool._deploymentContextMap.Add("1", "context");
        _context.ContextPool._contexts.Add("context", new() { Context = context, ConsumerCount = 1, });

        await _context.ContextPool.UnregisterDeploymentAsync("1");

        unloaded.Should().BeTrue();
        _context.ContextPool._contexts.Should().BeEmpty();
        _context.ContextPool._deploymentContextMap.Should().BeEmpty();
        _context.Logger.Calls.Should().Be(2);
        _context.Logger.LogLevel.Should().Be(LogLevel.Debug);
        _context.Logger.EventId.Should().Be(new EventId(3, nameof(ContextPoolLog.ContextUnloadInitiated)));
        _context.Logger.Exception.Should().BeNull();
        _context.Logger.Message.Should().MatchEquivalentOf($"*unload*of*context*{context.Name}*initiated*deployment*1*");
    }

    [Fact]
    public async Task Does_nothing_if_context_does_not_exist_Async()
    {
        await _context.ContextPool.UnregisterDeploymentAsync("1");
        _context.Logger.Calls.Should().Be(0);
    }
}

public sealed class ContextPool_GetUniqueHash
{
    [Fact]
    public void Recognize_different_lists()
    {
        var packageReferences1 = new List<PackageReference>()
        {
            new()
            {
                Name = "one",
                Version = "1.0.0",
            },
        };
        var packageReferences2 = new List<PackageReference>()
        {
            new()
            {
                Name = "two",
                Version = "1.0.0",
            },
        };
        var packageReferences3 = new List<PackageReference>()
        {
            new()
            {
                Name = "two",
                Version = "1.2.0",
            },
        };

        var hash1 = ContextPool.GetUniqueHash(packageReferences1);
        var hash2 = ContextPool.GetUniqueHash(packageReferences2);
        var hash3 = ContextPool.GetUniqueHash(packageReferences3);

        hash1.Should().NotBe(hash2);
        hash3.Should().NotBe(hash2);
    }

    [Fact]
    public void Ignores_order()
    {
        var packageReferences1 = new List<PackageReference>()
        {
            new()
            {
                Name = "one",
                Version = "1.0.0",
            },
            new()
            {
                Name = "two",
                Version = "1.0.0",
            },
        };
        var packageReferences2 = new List<PackageReference>()
        {
            new()
            {
                Name = "two",
                Version = "1.0.0",
            },
            new()
            {
                Name = "one",
                Version = "1.0.0",
            },
        };

        var hash1 = ContextPool.GetUniqueHash(packageReferences1);
        var hash2 = ContextPool.GetUniqueHash(packageReferences2);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Ignores_casing()
    {
        var packageReferences1 = new List<PackageReference>()
        {
            new()
            {
                Name = "one",
                Version = "1.0.0",
            },
        };
        var packageReferences2 = new List<PackageReference>()
        {
            new()
            {
                Name = "ONE",
                Version = "1.0.0",
            },
        };

        var hash1 = ContextPool.GetUniqueHash(packageReferences1);
        var hash2 = ContextPool.GetUniqueHash(packageReferences2);

        hash1.Should().Be(hash2);
    }
}

public sealed class ContextPool_AreEqualOrSubsetOfPackageReferences
{
    [Theory]
    [MemberData(nameof(GetEqualOrSubsetLists))]
    [SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable",
        Justification = "Xunit Serialisierung könnte die Tests beeinflussen.")]
    public void Recognize_equal_or_subset_lists(List<PackageReference> mainReferences, List<PackageReference> currentReferences)
        => ContextPool.AreEqualOrSubsetOfPackageReferences(mainReferences, currentReferences).Should().BeTrue();

    [Theory]
    [MemberData(nameof(GetDifferentLists))]
    [SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable",
        Justification = "Xunit Serialisierung könnte die Tests beeinflussen.")]
    public void Recognize_different_lists(List<PackageReference> mainReferences, List<PackageReference> currentReferences)
        => ContextPool.AreEqualOrSubsetOfPackageReferences(mainReferences, currentReferences).Should().BeFalse();

    public static TheoryData<List<PackageReference>, List<PackageReference>> GetEqualOrSubsetLists()
        => new()
        {
            {
                new List<PackageReference>()
                {
                    new()
                    {
                        Name = "one",
                        Version = "1.0.0",
                    },
                },
                new List<PackageReference>()
                {
                    new()
                    {
                        Name = "one",
                        Version = "1.0.0",
                    },
                }
            },
            {
                new List<PackageReference>()
                {
                    new()
                    {
                        Name = "one",
                        Version = "1.0.0",
                    },
                    new()
                    {
                        Name = "two",
                        Version = "1.0.0",
                    },
                },
                new List<PackageReference>()
                {
                    new()
                    {
                        Name = "one",
                        Version = "1.0.0",
                    },
                }
            },
        };

    public static TheoryData<List<PackageReference>, List<PackageReference>> GetDifferentLists()
        => new()
        {
            {
                new List<PackageReference>()
                {
                    new()
                    {
                        Name = "one",
                        Version = "1.0.0",
                    },
                },
                new List<PackageReference>()
                {
                    new()
                    {
                        Name = "one",
                        Version = "1.2.0",
                    },
                }
            },
            {
                new List<PackageReference>()
                {
                    new()
                    {
                        Name = "one",
                        Version = "1.0.0",
                    },
                },
                new List<PackageReference>()
                {
                    new()
                    {
                        Name = "two",
                        Version = "1.0.0",
                    },
                }
            },
            {
                new List<PackageReference>()
                {
                    new()
                    {
                        Name = "one",
                        Version = "1.0.0",
                    },
                },
                new List<PackageReference>()
            },
        };

    [Fact]
    public void Ignores_order()
    {
        var packageReferences1 = new List<PackageReference>()
        {
            new()
            {
                Name = "one",
                Version = "1.0.0",
            },
            new()
            {
                Name = "two",
                Version = "1.0.0",
            },
        };
        var packageReferences2 = new List<PackageReference>()
        {
            new()
            {
                Name = "two",
                Version = "1.0.0",
            },
            new()
            {
                Name = "one",
                Version = "1.0.0",
            },
        };

        ContextPool.AreEqualOrSubsetOfPackageReferences(packageReferences1, packageReferences2).Should().BeTrue();
    }

    [Fact]
    public void Ignores_casing()
    {
        var packageReferences1 = new List<PackageReference>()
        {
            new()
            {
                Name = "one",
                Version = "1.0.0",
            },
        };
        var packageReferences2 = new List<PackageReference>()
        {
            new()
            {
                Name = "ONE",
                Version = "1.0.0",
            },
        };

        ContextPool.AreEqualOrSubsetOfPackageReferences(packageReferences1, packageReferences2).Should().BeTrue();
    }
}

internal sealed class ContextPoolContext : IDisposable
{
    internal ContextPool ContextPool { get; }
    internal DirectoryMock Directory { get; } = new();
    internal IOptions<HostConfig> Config { get; } = Substitute.For<IOptions<HostConfig>>();
    internal TestLogger<ContextPool> Logger { get; } = new();

    internal ContextPoolContext()
    {
        Config.Value.Returns(new HostConfig { DeploymentsDirectory = Directory.Path, });
        ContextPool = new(Config, Substitute.For<ILoggerFactory>(), Directory.FileSystem, Logger);
    }

    public void Dispose() => ContextPool.Dispose();
}
