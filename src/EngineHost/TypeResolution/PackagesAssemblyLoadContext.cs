using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.TypeResolution;

internal sealed class PackagesAssemblyLoadContext : AssemblyLoadContext, IAssemblyLoadContext
{
    private readonly ConcurrentDictionary<string, WeakReference<Assembly>?> _managedAssembliesCache = [];
    private readonly ConcurrentDictionary<string, IntPtr> _unmanagedAssembliesCache = [];
    private readonly ConcurrentStack<IntPtr> _unmanagedAssemblies = new();
    private readonly IEnumerable<string> _sharedAssemblies;
    private readonly ILogger<PackagesAssemblyLoadContext> _logger;
    private readonly Dictionary<string, AssemblyDependencyResolver> _resolverMap;
    private readonly string _name;
    private readonly IFileSystem _fileSystem;

    private readonly ILoadContextMethods _loadContextMethods;

    internal PackagesAssemblyLoadContext(string engine, IReadOnlyCollection<string> sharedAssemblies, ILogger<PackagesAssemblyLoadContext> logger,
        List<(string Filename, string ComponentName)> files, IFileSystem fileSystem, ILoadContextMethods? loadContextMethods = null)
        : base(engine, true)
    {
        _loadContextMethods = loadContextMethods ?? new LoadContextMethods(this);
        _sharedAssemblies = sharedAssemblies;
        _logger = logger;
        _name = engine;
        _fileSystem = fileSystem;
        _resolverMap = GetResolverMap(files);
    }

    /// <remarks>Must be called before <c>Unload()</c> to transfer ownership of native handles to the caller.</remarks>
    internal IntPtr[] DetachNativeHandles()
    {
        var snapshot = _unmanagedAssemblies.ToArray();
        _unmanagedAssemblies.Clear();
        _unmanagedAssembliesCache.Clear();
        return snapshot;
    }

    internal static Dictionary<string, AssemblyDependencyResolver> GetResolverMap(List<(string Filename, string ComponentName)> files)
    {
        var resolvers = files
            .DistinctBy(e => e.ComponentName)
            .ToDictionary(e => e.ComponentName, e => new AssemblyDependencyResolver(e.ComponentName));
        return files.ToDictionary(e => e.Filename, e => resolvers[e.ComponentName]);
    }

    Assembly? IAssemblyLoadContext.Load(AssemblyName assemblyName)
        => Load(assemblyName);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var stackTrace = string.Empty;
        if (_logger.IsEnabled(LogLevel.Trace))
            stackTrace = CreateStackTraceMessage(new StackTrace(3, false), 5);

        if (_managedAssembliesCache.TryGetValue(assemblyName.FullName, out var cachedEntry))
        {
            _logger.LoadAssemblyFromCache(_name, assemblyName.FullName, stackTrace);
            return cachedEntry?.TryGetTarget(out var cachedAssembly) == true ? cachedAssembly : null;
        }

        if (_sharedAssemblies.Any(a => string.Equals(assemblyName.Name, a, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LoadAssemblyFromSharedAssemblies(_name, assemblyName.FullName, stackTrace);
            _managedAssembliesCache.TryAdd(assemblyName.FullName, null);
            return null;
        }

        if (_resolverMap.TryGetValue(assemblyName.Name ?? string.Empty, out var resolver))
        {
            var path = resolver.ResolveAssemblyToPath(assemblyName);
            if (path is not null)
            {
                _logger.LoadAssemblyFromFile(_name, assemblyName.FullName, path, stackTrace);
                var assembly = _loadContextMethods.LoadFromAssemblyPath(path);
                _managedAssembliesCache.TryAdd(assemblyName.FullName, new WeakReference<Assembly>(assembly));
                return assembly;
            }
        }

        _logger.LoadAssemblyFromDefaultContext(_name, assemblyName.FullName, stackTrace);
        _managedAssembliesCache.TryAdd(assemblyName.FullName, null);
        return null;
    }

    private static string CreateStackTraceMessage(StackTrace stackTrace, int limit)
        => Environment.NewLine + new StackTrace(stackTrace.GetFrames().Take(limit)).ToString();

    IntPtr IAssemblyLoadContext.LoadUnmanagedDll(string unmanagedDllName)
        => LoadUnmanagedDll(unmanagedDllName);

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var stackTrace = string.Empty;
        if (_logger.IsEnabled(LogLevel.Trace))
            stackTrace = CreateStackTraceMessage(new StackTrace(3, false), 5);

        if (_unmanagedAssembliesCache.TryGetValue(unmanagedDllName, out var pointer))
        {
            _logger.LoadNativeAssemblyFromCache(_name, unmanagedDllName, stackTrace);
            return pointer;
        }

        var filenameCandidates = GetPlatformSpecificCandidates(_fileSystem.Path.GetFileNameWithoutExtension(unmanagedDllName));

        foreach (var filename in filenameCandidates)
        {
            if (_resolverMap.TryGetValue(filename, out var resolver))
            {
                var path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

                if (path is not null && _loadContextMethods.TryLoadNativeLibrary(path, out var handle))
                {
                    if (_unmanagedAssembliesCache.TryAdd(unmanagedDllName, handle))
                    {
                        _unmanagedAssemblies.Push(handle);
                        _logger.LoadNativeAssemblyFromFile(_name, unmanagedDllName, path, stackTrace);
                        return handle;
                    }
                    _loadContextMethods.FreeNativeLibrary(handle);
                    return _unmanagedAssembliesCache.TryGetValue(unmanagedDllName, out var result) ? result : IntPtr.Zero;
                }
            }
        }

        _logger.LoadNativeAssemblyFromDefaultContext(_name, unmanagedDllName, stackTrace);
        return IntPtr.Zero;
    }

    // https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading#library-name-variations
    private static string[] GetPlatformSpecificCandidates(string name)
    {
        if (OperatingSystem.IsLinux())
        {
            return
            [
                name,
                $"lib{name}",
                $"{name}.so",
                $"lib{name}.so"
            ];
        }
        else if (OperatingSystem.IsWindows())
        {
            return [name];
        }
        throw new PlatformNotSupportedException();
    }
}
