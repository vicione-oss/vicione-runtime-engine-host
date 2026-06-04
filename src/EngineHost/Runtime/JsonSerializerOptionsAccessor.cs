using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ViciOne.ManagedEngine.Runtime;

/// <summary>
/// Provides access to internal members of `JsonSerializerOptions` for cache management purposes. This is necessary to ensure that cached options can be cleared when a deployment is removed, preventing potential memory leaks or unintended sharing of options across deployments.
/// This is normally used for hot-reload and is called by MetadataUpdateHandler.
/// <see href="https://github.com/dotnet/runtime/blob/main/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/JsonSerializerOptionsUpdateHandler.cs"/>
/// </summary>
internal static class JsonSerializerOptionsAccessor
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ClearCaches")]
    internal static extern void ClearCaches(this JsonSerializerOptions options);
}
