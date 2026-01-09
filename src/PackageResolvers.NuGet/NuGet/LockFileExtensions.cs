using System.IO;
using Microsoft.Extensions.DependencyModel;
using NuGet.ProjectModel;

namespace ViciOne.ManagedEngine.PackageResolver.NuGet;

internal static class LockFileExtensions
{
    internal static DependencyContext ToDependencyContext(this LockFile lockFile)
    {
        using MemoryStream streamIn = new();
        using StreamWriter textWriter = new(streamIn);
        textWriter.Write(new LockFileFormat().Render(lockFile));
        textWriter.Flush();
        streamIn.Flush();
        streamIn.Position = 0;

        using DependencyContextJsonReader reader = new();
        var dependencyContext = reader.Read(streamIn);
        return dependencyContext;
    }
}
