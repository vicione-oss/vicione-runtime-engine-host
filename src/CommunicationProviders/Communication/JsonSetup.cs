using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ManagedEngine.Communication;

public static class JsonSetup
{
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
