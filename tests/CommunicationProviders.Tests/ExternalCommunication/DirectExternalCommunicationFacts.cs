using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.ExternalCommunication;

public class DirectExternalCommunication_
{
    [Fact]
    public async Task Can_receive_transmitted_primitive_Async()
    {
        var channel = Guid.NewGuid().ToString();
        using DirectExchange exchange = new();
        TestLogger<DirectExternalIncomingCommunication> incomingLogger = new(LogLevel.Warning);
        var incoming = new DirectExternalIncomingCommunication(incomingLogger, exchange,
            new()
            {
                EngineUniqueIdentifier = "A",
                ConnectionUniqueIdentifier = "E",
                Channels = [channel],
            }, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        TestLogger<DirectExternalOutgoingCommunication> outgoingLogger = new(LogLevel.Warning);
        var outgoing = new DirectExternalOutgoingCommunication(
            new()
            {
                EngineUniqueIdentifier = "B",
                ConnectionUniqueIdentifier = "E",
            }, exchange, outgoingLogger, Substitute.For<INameResolver>(), new("other"));

        try
        {
            await incoming.ConnectAsync(CancellationToken.None);
            await outgoing.ConnectAsync(CancellationToken.None);

            using var completionTimeOut = new ManualResetEvent(false);
            List<ExternalValue> receivedValues = [];
            incoming.Received += values =>
            {
                receivedValues.AddRange(values);
                completionTimeOut.Set();
            };

            var value = 1.23;
            await outgoing.SendAsync(8,
                [
                    new()
                    {
                        Timestamp = DateTime.UtcNow,
                        Channel = channel,
                        Validity = 1,
                        Value = value,
                    },
                ], CancellationToken.None);

            completionTimeOut.WaitOne(5000);

            outgoingLogger.Exception.Should().BeNull();
            outgoingLogger.Calls.Should().Be(0);
            incomingLogger.Exception.Should().BeNull();
            incomingLogger.Calls.Should().Be(0);
            receivedValues.Should().ContainSingle().Which.Value.Should().Be(value);
        }
        finally
        {
            await incoming.DisconnectAsync(CancellationToken.None);
            await outgoing.DisconnectAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Can_receive_transmitted_complex_data_Async()
    {
        var channel = Guid.NewGuid().ToString();
        using DirectExchange exchange = new();
        TestLogger<DirectExternalIncomingCommunication> incomingLogger = new(LogLevel.Warning);
        var incoming = new DirectExternalIncomingCommunication(incomingLogger, exchange,
            new()
            {
                EngineUniqueIdentifier = "A",
                ConnectionUniqueIdentifier = "E",
                Channels = [channel],
            }, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        TestLogger<DirectExternalOutgoingCommunication> outgoingLogger = new(LogLevel.Warning);
        var outgoing = new DirectExternalOutgoingCommunication(
            new()
            {
                EngineUniqueIdentifier = "B",
                ConnectionUniqueIdentifier = "E",
            }, exchange, outgoingLogger, Substitute.For<INameResolver>(), new("other"));

        try
        {
            await incoming.ConnectAsync(CancellationToken.None);
            await outgoing.ConnectAsync(CancellationToken.None);

            using var completionTimeOut = new ManualResetEvent(false);
            List<ExternalValue> receivedValues = [];
            incoming.Received += values =>
            {
                receivedValues.AddRange(values);
                completionTimeOut.Set();
            };

            IBase value = new Truck()
            {
                HeavyDuty = true,
                Children =
                [
                    new Car()
                    {
                        Seats = 2,
                        Children =
                        [
                            new Passenger() { Age = 18, },
                        ],
                    },
                ],
            };
            await outgoing.SendAsync(8,
                [
                    new()
                    {
                        Timestamp = DateTime.UtcNow,
                        Channel = channel,
                        Validity = 1,
                        Value = value,
                    },
                ], CancellationToken.None);

            completionTimeOut.WaitOne(5000);

            outgoingLogger.Exception.Should().BeNull();
            outgoingLogger.Calls.Should().Be(0);
            incomingLogger.Exception.Should().BeNull();
            incomingLogger.Calls.Should().Be(0);
            receivedValues.Should().ContainSingle().Which.Value.Should().BeEquivalentTo(value, o => o.PreferringRuntimeMemberTypes());
        }
        finally
        {
            await incoming.DisconnectAsync(CancellationToken.None);
            await outgoing.DisconnectAsync(CancellationToken.None);
        }
    }
}
