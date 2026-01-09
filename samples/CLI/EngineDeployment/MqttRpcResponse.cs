using System;
using System.IO;
using System.IO.Compression;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace ViciOne.ManagedEngine.EngineDeployment;

internal sealed record class MqttRpcResponse(string Status)
{
    public ArraySegment<byte>? Payload { get; set; }
    public string? ContentType { get; set; }

    public MqttRpcResponse(string status, ArraySegment<byte> payload, string contentType)
        : this(status)
    {
        Payload = payload;
        ContentType = contentType;
    }

    public string PlainText()
    {
        if (ContentType != MediaTypeNames.Text.Plain)
            throw new InvalidOperationException($"Plain text cannot be parsed from content of type '{ContentType}'.");

        if (!Payload.HasValue)
            throw new InvalidOperationException($"Plain text cannot be parsed from not existing payload.");

        return Encoding.UTF8.GetString(Payload.Value);
    }

    public T GZipJson<T>(JsonSerializerOptions? options = default)
    {
        if (ContentType != MediaTypeNames.Application.GZip)
            throw new InvalidOperationException($"GZip JSON cannot be parsed from content of type '{ContentType}'.");

        if (!Payload.HasValue)
            throw new InvalidOperationException($"GZip JSON cannot be parsed from not existing payload.");

        using MemoryStream memory = new(Payload.Value.Array ?? []);
        using GZipStream gzip = new(memory, CompressionMode.Decompress);
        try
        {
            var result = JsonSerializer.Deserialize<T>(gzip, options);
            return result is null ? throw CannotParseMqttRpcResponse("<gzip compressed content>") : result;
        }
        catch (JsonException ex)
        {
            throw CannotParseMqttRpcResponse("<gzip compressed content>", ex);
        }
    }

    public T Json<T>(JsonSerializerOptions? options = default)
    {
        if (ContentType != MediaTypeNames.Application.Json)
            throw new InvalidOperationException($"JSON cannot be parsed from content of type '{ContentType}'.");

        if (!Payload.HasValue)
            throw new InvalidOperationException($"JSON cannot be parsed from not existing payload.");

        try
        {
            var result = JsonSerializer.Deserialize<T>(Payload.Value, options);
            return result is null ? throw CannotParseMqttRpcResponse(Payload.Value) : result;
        }
        catch (JsonException ex)
        {
            throw CannotParseMqttRpcResponse(Payload.Value, ex);
        }
    }

    private static InvalidOperationException CannotParseMqttRpcResponse(ArraySegment<byte> payload, Exception? exception = default)
    {
        var payloadText = Encoding.UTF8.GetString(payload);
        throw CannotParseMqttRpcResponse(payloadText, exception);
    }

    private static InvalidOperationException CannotParseMqttRpcResponse(string? text, Exception? exception = default)
        => new($"Cannot parse MQTT RPC response '{text}'", exception);
}
