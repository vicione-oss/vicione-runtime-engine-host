using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Extensions;
using NSubstitute;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.ExternalCommunication;

public class MqttExternalCommunication_
{
    [Fact]
    public async Task Can_receive_transmitted_primitive()
    {
        const int Port = 1886;
        MqttCommunication communication = new() { Host = "localhost", Port = Port, };
        var mqttClient = Substitute.For<IVirtualMqttClient>();
        TestLogger<MqttExternalOutgoingCommunication> outgoingLogger = new(LogLevel.Warning);
        using var outgoing = new MqttExternalOutgoingCommunication(communication, new(), mqttClient, outgoingLogger, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        TestLogger<MqttExternalIncomingCommunication> incomingLogger = new(LogLevel.Warning);
        using var incoming = new MqttExternalIncomingCommunication(communication, incomingLogger, new() { Channels = ["my.topic"], }, mqttClient, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);

        mqttClient.When(x => x.Publish(Arg.Any<MqttApplicationMessage>()))
            .Do(callInfo => mqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(
                new MqttApplicationMessageReceivedEventArgs(string.Empty, callInfo.Arg<MqttApplicationMessage>(), new(), (_1, _2) => Task.CompletedTask)));

        try
        {
            var connectTask = Task.WhenAll(
                outgoing.ConnectAsync(CancellationToken.None),
                incoming.ConnectAsync(CancellationToken.None));
            using CancellationTokenSource tokenSource = new();
            var completedFirst = await Task.WhenAny(connectTask, Task.Delay(5000, tokenSource.Token));
            completedFirst.Should().BeSameAs(connectTask);
            await tokenSource.CancelAsync();

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
                        Channel = "my.topic",
                        Validity = 1,
                        Value = value,
                    },
                ], CancellationToken.None);

            completionTimeOut.WaitOne(10_000);

            outgoingLogger.Exception.Should().BeNull();
            outgoingLogger.Calls.Should().Be(0);
            incomingLogger.Exception.Should().BeNull();
            incomingLogger.Calls.Should().Be(0);
            receivedValues.Should().ContainSingle().Which.Value.Should().Be(value);
        }
        finally
        {
            await Task.WhenAll(
                incoming.DisconnectAsync(CancellationToken.None),
                outgoing.DisconnectAsync(CancellationToken.None));
        }
    }

    [Fact]
    public async Task Can_receive_transmitted_complex_data()
    {
        const int Port = 1887;
        MqttCommunication communication = new() { Host = "localhost", Port = Port, };
        var mqttClient = Substitute.For<IVirtualMqttClient>();
        TestLogger<MqttExternalOutgoingCommunication> outgoingLogger = new(LogLevel.Warning);
        using var outgoing = new MqttExternalOutgoingCommunication(communication, new(), mqttClient, outgoingLogger, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        TestLogger<MqttExternalIncomingCommunication> incomingLogger = new(LogLevel.Warning);
        using var incoming = new MqttExternalIncomingCommunication(communication, incomingLogger, new() { Channels = ["my.topic"], }, mqttClient, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);

        mqttClient.When(x => x.Publish(Arg.Any<MqttApplicationMessage>()))
            .Do(callInfo => mqttClient.MessageReceived += Raise.Event<Func<MqttApplicationMessageReceivedEventArgs, Task>>(
                new MqttApplicationMessageReceivedEventArgs(string.Empty, callInfo.Arg<MqttApplicationMessage>(), new(), (_1, _2) => Task.CompletedTask)));

        try
        {
            var connectTask = Task.WhenAll(
                outgoing.ConnectAsync(CancellationToken.None),
                incoming.ConnectAsync(CancellationToken.None));
            using CancellationTokenSource tokenSource = new();
            var completedFirst = await Task.WhenAny(connectTask, Task.Delay(5000, tokenSource.Token));
            completedFirst.Should().BeSameAs(connectTask);
            await tokenSource.CancelAsync();

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
                        Channel = "my.topic",
                        Validity = 1,
                        Value = value,
                    },
                ], CancellationToken.None);

            completionTimeOut.WaitOne(10_000);

            outgoingLogger.Exception.Should().BeNull();
            outgoingLogger.Calls.Should().Be(0);
            incomingLogger.Exception.Should().BeNull();
            incomingLogger.Calls.Should().Be(0);
            receivedValues.Should().ContainSingle().Which.Value.Should().BeEquivalentTo(value, o => o.PreferringRuntimeMemberTypes());
        }
        finally
        {
            await Task.WhenAll(
                incoming.DisconnectAsync(CancellationToken.None),
                outgoing.DisconnectAsync(CancellationToken.None));
        }
    }
}
