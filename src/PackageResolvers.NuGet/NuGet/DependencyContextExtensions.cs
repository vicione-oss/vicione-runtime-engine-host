using System.IO;
using Microsoft.Extensions.DependencyModel;

namespace ViciOne.ManagedEngine.PackageResolver.NuGet;

internal static class DependencyContextExtensions
{
    internal static string ToJson(this DependencyContext dependencyContext)
    {
        using MemoryStream streamOut = new();
        DependencyContextWriter writer = new();
        writer.Write(dependencyContext, streamOut);
        streamOut.Flush();
        streamOut.Position = 0;

        using StreamReader streamReader = new(streamOut);
        return streamReader.ReadToEnd();
    }
}
