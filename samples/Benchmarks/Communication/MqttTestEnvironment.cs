using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ManagedEngine.Communication;

namespace Benchmarks.Communication;

[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Wird mit BenchmarkDotnet als Benchmark-Param genutzt")]
public sealed record class MqttTestEnvironment(bool InProcessBroker, Uri? Uri, string? Host, int? Port, short QoS)
{
    public static IEnumerable<MqttTestEnvironment> DefaultSet()
    {
        yield return new(true, default, "localhost", 1884, 0);
        yield return new(true, default, "localhost", 1884, 1);
        yield return new(true, default, "localhost", 1884, 2);
        yield return new(false, default, "localhost", 1883, 0);
        yield return new(false, default, "localhost", 1883, 1);
        yield return new(false, default, "localhost", 1883, 2);
        yield return new(false, default, "test.mosquitto.org", 1883, 0);
        yield return new(false, default, "test.mosquitto.org", 1883, 1);
        yield return new(false, default, "test.mosquitto.org", 1883, 2);
        yield return new(false, new Uri("localhost:8000/mqtt"), default, default, 0);
        yield return new(false, new Uri("localhost:8000/mqtt"), default, default, 1);
        yield return new(false, new Uri("localhost:8000/mqtt"), default, default, 2);
    }

    internal MqttCommunication ToMqttCommunication()
    {
        if (Uri is not null)
            return new() { Uri = Uri, QualityOfService = QoS, };
        else if (!string.IsNullOrWhiteSpace(Host) && Port.HasValue)
            return new() { Host = Host, Port = Port.Value, QualityOfService = QoS, };
        else
            throw new InvalidOperationException($"Invalid {nameof(MqttTestEnvironment)} configuration.");
    }
}
