using System;
using System.Reflection;

namespace ViciOne.ManagedEngine.TypeResolution;

internal interface ILoadContextMethods
{
    Assembly LoadFromAssemblyPath(string path);
    bool TryLoadNativeLibrary(string path, out IntPtr handle);
    void FreeNativeLibrary(IntPtr handle);
}
