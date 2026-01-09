using System.Text.Json.Serialization;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine;

[JsonSerializable(typeof(InfoMessage))]
[JsonSerializable(typeof(CrashMessage))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
