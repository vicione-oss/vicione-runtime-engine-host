using System;
using System.Reflection;
using System.Text.Json;

namespace ViciOne.ManagedEngine.Runtime;

/// <summary>
/// Provides access to internal members of `JsonSerializer` for cache management purposes. This is necessary to ensure that cached members can be cleared when a deployment is removed, preventing potential memory leaks or unintended sharing of members across deployments.
/// This is normally used for hot-reload and is called by MetadataUpdateHandler.
/// <see href="https://github.com/dotnet/runtime/blob/main/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/JsonSerializerOptionsUpdateHandler.cs"/>
/// </summary>
internal static class JsonMemberAccessor
{
    private static readonly MethodInfo s_clearCacheMethod =
#if NET11_0_OR_GREATER
        typeof(JsonSerializerOptions).Assembly
            .GetType("System.Text.Json.Serialization.Metadata.MemberAccessor", throwOnError: true)!
            .GetMethod("ClearCache", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Cannot find ClearCache method on System.Text.Json.Serialization.Metadata.MemberAccessor");
#else
        typeof(JsonSerializerOptions).Assembly
            .GetType("System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver", throwOnError: true)
            ?.GetMethod("ClearMemberAccessorCaches", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Cannot find ClearMemberAccessorCaches method on System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver");
#endif

    internal static void ClearCache() => s_clearCacheMethod?.Invoke(null, null);
}
