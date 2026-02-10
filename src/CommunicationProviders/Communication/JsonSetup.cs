using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ManagedEngine.Communication;

/// <summary>
/// Provides JSON serialization configuration utilities.
/// </summary>
public static class JsonSetup
{
    /// <summary>
    /// Creates JSON serializer options configured for type preservation.
    /// </summary>
    /// <param name="assemblyLoadContext">The assembly load context for type resolution.</param>
    /// <returns>Configured JSON serializer options.</returns>
    public static JsonSerializerOptions CreatePreserveTypeOptions(AssemblyLoadContext? assemblyLoadContext = default)
        => new()
        {
            Converters =
            {
                new JsonStringEnumConverter(),
                new TypeJsonConverter(assemblyLoadContext),
                new TypeNameHandlingConfig(assemblyLoadContext),
            },
            ReferenceHandler = ReferenceHandler.Preserve,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        };
}
