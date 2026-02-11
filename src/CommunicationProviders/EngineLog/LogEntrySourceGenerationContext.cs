using System.Text.Json.Serialization;

namespace ViciOne.ManagedEngine.EngineLog;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(LogEntry))]
internal partial class LogEntrySourceGenerationContext : JsonSerializerContext;
