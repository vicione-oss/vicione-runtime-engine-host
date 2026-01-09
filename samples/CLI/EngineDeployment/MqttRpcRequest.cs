using System;
using System.IO;
using System.IO.Compression;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace ViciOne.ManagedEngine.EngineDeployment;

internal sealed record class MqttRpcRequest(string Method)
{
    internal ArraySegment<byte>? Payload { get; set; }
    internal string? ContentType { get; set; }

    internal MqttRpcRequest(string method, ArraySegment<byte> payload, string contentType)
        : this(method)
    {
        Payload = payload;
        ContentType = contentType;
    }

    internal static MqttRpcRequest PlainText(string method, string text)
        => new(method, new(Encoding.UTF8.GetBytes(text)), MediaTypeNames.Text.Plain);

    internal static MqttRpcRequest GZipJson(string method)
        => new(method, ArraySegment<byte>.Empty, MediaTypeNames.Application.GZip);

    internal static MqttRpcRequest GZipJson<T>(string method, T payload, JsonSerializerOptions? options = default)
    {
        using MemoryStream memory = new();
        using (GZipStream gzip = new(memory, CompressionMode.Compress))
            JsonSerializer.Serialize(gzip, payload, options);
        return new(method, new(memory.ToArray()), MediaTypeNames.Application.GZip);
    }

    internal static MqttRpcRequest Json(string method)
        => new(method, ArraySegment<byte>.Empty, MediaTypeNames.Application.Json);

    internal static MqttRpcRequest Json<T>(string method, T payload, JsonSerializerOptions? options = default)
        => new(method, new(JsonSerializer.SerializeToUtf8Bytes(payload, options)), MediaTypeNames.Application.Json);
}
