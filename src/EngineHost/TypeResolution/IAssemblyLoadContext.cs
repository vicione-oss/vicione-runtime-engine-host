using System;
using System.Reflection;

namespace ViciOne.ManagedEngine.TypeResolution;

internal interface IAssemblyLoadContext
{
    Assembly? Load(AssemblyName assemblyName);
    IntPtr LoadUnmanagedDll(string unmanagedDllName);
}
