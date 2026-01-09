using System;
using System.Collections.Generic;
using System.IO;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Testably.Abstractions.Testing;
using Xunit;

namespace ViciOne.ManagedEngine.TypeResolution;

public class PackagesAssemblyLoadContext_Load // can not be abstracted
{
    [Fact]
    public void Returns_null_if_assembly_is_unknown()
    {
        var loadContextMethods = Substitute.For<ILoadContextMethods>();
        TestLogger<PackagesAssemblyLoadContext> logger = new();
        IAssemblyLoadContext context = new PackagesAssemblyLoadContext("my-engine", [], logger, [], new MockFileSystem(), loadContextMethods);

        var result = context.Load(new("System.Drawing.Point, Version=9.0.0.0, Culture=neutral, PublicKeyToken=null"));

        result.Should().BeNull();
        loadContextMethods.DidNotReceive().LoadFromAssemblyPath(Arg.Any<string>());
        logger.Message.Should().MatchEquivalentOf("*from*default*context*");
    }

    [Fact]
    public void Returns_null_if_assembly_is_shared()
    {
        var loadContextMethods = Substitute.For<ILoadContextMethods>();
        TestLogger<PackagesAssemblyLoadContext> logger = new();
        IAssemblyLoadContext context = new PackagesAssemblyLoadContext("my-engine", ["System.Drawing.Point"], logger, [], new MockFileSystem(), loadContextMethods);

        var result = context.Load(new("System.Drawing.Point, Version=9.0.0.0, Culture=neutral, PublicKeyToken=null"));

        result.Should().BeNull();
        loadContextMethods.DidNotReceive().LoadFromAssemblyPath(Arg.Any<string>());
        logger.Message.Should().MatchEquivalentOf("*from*shared*assemblies*");
    }

    [Fact]
    public void Returns_assembly_from_file()
    {
        var loadContextMethods = Substitute.For<ILoadContextMethods>();
        TestLogger<PackagesAssemblyLoadContext> logger = new();
        using TemporaryDirectory directory = new(false);
        var componentDirectory = directory.CreateDirectory("Component1");
        var file = directory.CreateFile("Component1", "System.Drawing.Point.dll");
        IAssemblyLoadContext context = new PackagesAssemblyLoadContext("my-engine", [], logger,
            [
                ("System.Drawing.Point", file),
            ], directory.FileSystem, loadContextMethods);

        var result = context.Load(new("System.Drawing.Point, Version=9.0.0.0, Culture=neutral, PublicKeyToken=null"));

        loadContextMethods.Received().LoadFromAssemblyPath(Arg.Is<string>(s => s.EndsWith("System.Drawing.Point.dll")));
        logger.Message.Should().MatchEquivalentOf("*from*file*Component1*Point.dll*");
    }

    [Fact]
    public void Returns_cached_assembly()
    {
        var loadContextMethods = Substitute.For<ILoadContextMethods>();
        TestLogger<PackagesAssemblyLoadContext> logger = new();
        using TemporaryDirectory directory = new(false);
        var componentDirectory = directory.CreateDirectory("Component1");
        var file = directory.CreateFile("Component1", "System.Drawing.Point.dll");
        IAssemblyLoadContext context = new PackagesAssemblyLoadContext("my-engine", [], logger,
            [
                ("System.Drawing.Point", file),
            ], directory.FileSystem, loadContextMethods);

        context.Load(new("System.Drawing.Point, Version=9.0.0.0, Culture=neutral, PublicKeyToken=null"));
        context.Load(new("System.Drawing.Point, Version=9.0.0.0, Culture=neutral, PublicKeyToken=null"));

        loadContextMethods.Received(1).LoadFromAssemblyPath(Arg.Is<string>(s => s.EndsWith("System.Drawing.Point.dll")));
        logger.Message.Should().MatchEquivalentOf("*System.Drawing.Point*cache*");
    }
}

public class PackagesAssemblyLoadContext_LoadUnmanagedDll
{
    [Fact]
    public void Returns_zero_if_assembly_is_unknown()
    {
        var loadContextMethods = Substitute.For<ILoadContextMethods>();
        TestLogger<PackagesAssemblyLoadContext> logger = new();
        IAssemblyLoadContext context = new PackagesAssemblyLoadContext("my-engine", [], logger, [], new MockFileSystem(), loadContextMethods);

        var result = context.LoadUnmanagedDll("libc");

        result.Should().Be(IntPtr.Zero);
        loadContextMethods.DidNotReceive().TryLoadNativeLibrary(Arg.Any<string>(), out Arg.Any<nint>());
        logger.Message.Should().MatchEquivalentOf("*native*from*default*context*");
    }

    [Fact]
    public void Returns_cached_pointer()
    {
        var loadContextMethods = Substitute.For<ILoadContextMethods>();
        TestLogger<PackagesAssemblyLoadContext> logger = new();
        using TemporaryDirectory directory = new(false);
        var componentDirectory = directory.CreateDirectory("Component1");
        var file = directory.CreateFile("Component1", "libc");
        loadContextMethods.TryLoadNativeLibrary(Arg.Any<string>(), out Arg.Any<nint>())
            .Returns(c =>
            {
                c[1] = IntPtr.MaxValue;
                return true;
            });
        IAssemblyLoadContext context = new PackagesAssemblyLoadContext("my-engine", [], logger,
            [
                ("libc", file),
            ], directory.FileSystem, loadContextMethods);

        var result1 = context.LoadUnmanagedDll("libc");
        var result2 = context.LoadUnmanagedDll("libc");

        result1.Should().Be(result2).And.Be(IntPtr.MaxValue);
        loadContextMethods.Received(1).TryLoadNativeLibrary(Arg.Is<string>(s => s.EndsWith("libc")), out Arg.Any<nint>());
        logger.Message.Should().MatchEquivalentOf("*native*libc*cache*");
    }

    [Fact]
    public void Returns_pointer_from_loaded_assembly_windows()
    {
        if (OperatingSystem.IsWindows())
        {
            ReturnsPointerFromLoadedAssembly("libc", "libc", "libc");
            ReturnsPointerFromLoadedAssembly("libc", "libc.dll", "libc");
            ReturnsPointerFromLoadedAssembly("libc.dll", "libc.dll", "libc");
        }
        else if (OperatingSystem.IsLinux())
        {
            ReturnsPointerFromLoadedAssembly("libc", "libc", "libc");
            ReturnsPointerFromLoadedAssembly("libc", "libc.so", "libc");
            ReturnsPointerFromLoadedAssembly("libc.so", "libc.so", "libc");
            ReturnsPointerFromLoadedAssembly("libc.so.6", "libc.so.6", "libc.so");
            ReturnsPointerFromLoadedAssembly("c", "c", "c");
            ReturnsPointerFromLoadedAssembly("c", "c.so", "c");
            ReturnsPointerFromLoadedAssembly("c.so.6", "c.so.6", "c.so");
            ReturnsPointerFromLoadedAssembly("c", "libc", "libc");
            ReturnsPointerFromLoadedAssembly("c", "libc.so", "libc");
            ReturnsPointerFromLoadedAssembly("c.so", "libc.so", "libc");
            ReturnsPointerFromLoadedAssembly("c.so.6", "libc.so.6", "libc.so");
        }
    }

    [Fact]
    public void Returns_zero_if_name_file_combination_is_invalid()
    {
        if (OperatingSystem.IsWindows())
        {
            ReturnsZero("libc.dll", "libc", "libc");
        }
        else if (OperatingSystem.IsLinux())
        {
            ReturnsZero("libc", "libc.so.6", "libc.so");
            ReturnsZero("c", "c.so.6", "c.so");
            ReturnsZero("c", "libc.so.6", "libc.so");
            ReturnsZero("libc.so.6", "libc.so", "libc");
        }
    }

    private static void ReturnsPointerFromLoadedAssembly(string unmanagedDllName, string filename, string resolvedName)
    {
        var loadContextMethods = Substitute.For<ILoadContextMethods>();
        TestLogger<PackagesAssemblyLoadContext> logger = new();
        using TemporaryDirectory directory = new(false);
        var componentDirectory = directory.CreateDirectory("Component1");
        var file = directory.CreateFile("Component1", filename);
        loadContextMethods.TryLoadNativeLibrary(Path.GetFullPath(file), out Arg.Any<nint>())
            .Returns(c =>
            {
                c[1] = IntPtr.MaxValue;
                return true;
            });
        IAssemblyLoadContext context = new PackagesAssemblyLoadContext("my-engine", [], logger,
            [
                (resolvedName, file),
            ], directory.FileSystem, loadContextMethods);

        var result = context.LoadUnmanagedDll(unmanagedDllName);

        result.Should().Be(IntPtr.MaxValue);
        logger.Message.Should().MatchEquivalentOf($"*native*from*file*{filename}*");
    }

    private static void ReturnsZero(string unmanagedDllName, string filename, string resolvedName)
    {
        var loadContextMethods = Substitute.For<ILoadContextMethods>();
        TestLogger<PackagesAssemblyLoadContext> logger = new();
        using TemporaryDirectory directory = new(false);
        var componentDirectory = directory.CreateDirectory("Component1");
        var file = directory.CreateFile("Component1", filename);
        loadContextMethods.TryLoadNativeLibrary(Path.GetFullPath(file), out Arg.Any<nint>())
            .Returns(c =>
            {
                c[1] = IntPtr.MaxValue;
                return true;
            });
        IAssemblyLoadContext context = new PackagesAssemblyLoadContext("my-engine", [], logger,
            [
                (resolvedName, file),
            ], directory.FileSystem, loadContextMethods);

        var result = context.LoadUnmanagedDll(unmanagedDllName);

        result.Should().Be(IntPtr.Zero);
    }
}

public class PackagesAssemblyLoadContext_GetResolverMap
{
    [Fact]
    public void Returns_same_resolver_for_equal_components()
    {
        using TemporaryDirectory directory = new(false);
        var equalMainComponent = directory.CreateFile("A", "Main.dll");
        var differentMainComponent = directory.CreateFile("B", "Main.dll");
        AssemblyHelper.CreateAssembly(equalMainComponent, "Main.dll", "1.0.0", directory.FileSystem);
        AssemblyHelper.CreateAssembly(differentMainComponent, "Main.dll", "1.0.0", directory.FileSystem);

        List<(string Filename, string ComponentName)> files =
        [
            ("Assembly1", equalMainComponent),
            ("Assembly2", equalMainComponent),
            ("Assembly3", differentMainComponent),
        ];

        var map = PackagesAssemblyLoadContext.GetResolverMap(files);

        map.Should().HaveCount(3);
        map["Assembly1"].Should().Be(map["Assembly2"]).And.NotBe(map["Assembly3"]);
    }
}
