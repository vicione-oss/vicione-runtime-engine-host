using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace ViciOne.ManagedEngine.TypeResolution;

internal sealed class LoadContextMethods(AssemblyLoadContext loadContext) : ILoadContextMethods
{
    public Assembly LoadFromAssemblyPath(string path)
        => loadContext.LoadFromAssemblyPath(path);

    public bool TryLoadNativeLibrary(string path, out IntPtr handle)
        => NativeLibrary.TryLoad(path, out handle);

    public void FreeNativeLibrary(IntPtr handle)
        => NativeLibrary.Free(handle);
}
