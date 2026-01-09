using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.ExternalCommunication;

public class DirectExternalIncomingCommunication_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
    {
        await using ConstructorProviderFactory providerFactory = new();

        var instance = providerFactory.CreateExternalIncoming(
            new DirectCommunication(),
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<DirectExternalIncomingCommunication>();
    }
}

public class DirectExternalIncomingCommunication_ConnectAsync
{
    [Fact]
    public async Task Registers_subscriber_successfully_Async()
    {
        var logger = Substitute.For<ILogger<DirectExternalIncomingCommunication>>();
        using var exchange = new DirectExchange();
        var communication = new DirectExternalIncomingCommunication(logger, exchange, new() { Channels = { "A", } }, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);

        await communication.ConnectAsync(CancellationToken.None);

        var subscription = exchange._subscriptions.Should().ContainSingle().Which;
        subscription.Key.Should().Be("A");
        subscription.Value.Should().ContainSingle();
    }
}

public class DirectExternalIncomingCommunication_Received
{
    private readonly JsonSerializerOptions _serializerOptions = JsonSetup.CreatePreserveTypeOptions();

    [Fact]
    public async Task Handle_values_Async()
    {
        var logger = Substitute.For<ILogger<DirectExternalIncomingCommunication>>();
        using var exchange = new DirectExchange();
        var communication = new DirectExternalIncomingCommunication(logger, exchange,
            new()
            {
                Channels =
                [
                    "00000000-0000-0000-0000-000000000001",
                    "00000000-0000-0000-0000-000000000002"
                ],
            }, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        var receivedValues = new List<ExternalValue>();
        communication.Received += receivedValues.AddRange;
        await communication.ConnectAsync(CancellationToken.None);

        ExternalValue externalValue1 = new()
        {
            Timestamp = new DateTime(2022, 01, 27, 13, 53, 20),
            Channel = "00000000-0000-0000-0000-000000000001",
            Validity = 1,
            Value = 123.45,
        };
        ExternalValue externalValue2 = new()
        {
            Timestamp = new DateTime(2022, 01, 27, 13, 54, 20),
            Channel = "00000000-0000-0000-0000-000000000002",
            Validity = 0,
            Value = "Running...",
        };

        await exchange.ForwardValuesAsync([externalValue1, externalValue2,], AssemblyLoadContext.Default.GetHashCode(), _serializerOptions);

        receivedValues.Should().BeEquivalentTo(new ExternalValue[]
        {
            new()
            {
                Timestamp = new DateTime(2022, 01, 27, 13, 53, 20),
                Channel = "00000000-0000-0000-0000-000000000001",
                Validity = 1,
                Value = 123.45,
            },
            new()
            {
                Timestamp = new DateTime(2022, 01, 27, 13, 54, 20),
                Channel = "00000000-0000-0000-0000-000000000002",
                Validity = 0,
                Value = "Running...",
            },
        });
    }

    [Fact]
    public async Task Does_not_raise_received_for_empty_list_Async()
    {
        var logger = Substitute.For<ILogger<DirectExternalIncomingCommunication>>();
        using var exchange = new DirectExchange();
        var communication = new DirectExternalIncomingCommunication(logger, exchange, new(), Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        var called = false;
        communication.Received += _ => called = true;
        await communication.ConnectAsync(CancellationToken.None);

        await exchange.ForwardValuesAsync([], AssemblyLoadContext.Default.GetHashCode(), _serializerOptions);

        called.Should().BeFalse();
    }

    [Fact]
    public async Task Can_receive_null_value_Async()
    {
        var logger = Substitute.For<ILogger<DirectExternalIncomingCommunication>>();
        using var exchange = new DirectExchange();
        var communication = new DirectExternalIncomingCommunication(logger, exchange,
            new()
            {
                Channels = ["00000000-0000-0000-0000-000000000001",],
            }, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        var receivedValues = new List<ExternalValue>();
        communication.Received += receivedValues.AddRange;
        await communication.ConnectAsync(CancellationToken.None);

        ExternalValue externalValue = new()
        {
            Timestamp = new DateTime(2022, 01, 27, 13, 53, 20),
            Channel = "00000000-0000-0000-0000-000000000001",
            Validity = 1,
            Value = null,
        };

        await exchange.ForwardValuesAsync([externalValue,], -1, _serializerOptions);

        receivedValues.Should().BeEquivalentTo(new ExternalValue[]
        {
            new()
            {
                Timestamp = new DateTime(2022, 01, 27, 13, 53, 20),
                Channel = "00000000-0000-0000-0000-000000000001",
                Validity = 1,
                Value = null,
            },
        });
    }

    [Fact]
    public async Task Can_receive_complex_value_Async()
    {
        var logger = Substitute.For<ILogger<DirectExternalIncomingCommunication>>();
        using var exchange = new DirectExchange();
        var communication = new DirectExternalIncomingCommunication(logger, exchange,
            new()
            {
                Channels = ["00000000-0000-0000-0000-000000000001",],
            }, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        var receivedValues = new List<ExternalValue>();
        communication.Received += receivedValues.AddRange;
        await communication.ConnectAsync(CancellationToken.None);

        ExternalValue externalValue = new()
        {
            Timestamp = new DateTime(2022, 01, 27, 13, 53, 20),
            Channel = "00000000-0000-0000-0000-000000000001",
            Validity = 1,
            Value = new CancelEventArgs(true),
        };

        await exchange.ForwardValuesAsync([externalValue,], -1, _serializerOptions);

        receivedValues.Should().BeEquivalentTo(new ExternalValue[]
        {
            new()
            {
                Timestamp = new DateTime(2022, 01, 27, 13, 53, 20),
                Channel = "00000000-0000-0000-0000-000000000001",
                Validity = 1,
                Value = new CancelEventArgs(true),
            },
        });
    }
}

public class DirectExternalIncomingCommunication_DisconnectAsync
{
    [Fact]
    public async Task Removes_from_list_Async()
    {
        var logger = Substitute.For<ILogger<DirectExternalIncomingCommunication>>();
        using var exchange = new DirectExchange();
        var communication = new DirectExternalIncomingCommunication(logger, exchange, new() { Channels = { "A", }, }, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);

        await communication.ConnectAsync(CancellationToken.None);

        var subscription = exchange._subscriptions.Should().ContainSingle().Which;
        subscription.Key.Should().Be("A");
        subscription.Value.Should().ContainSingle();
        exchange._subscriptions.Add("test", []);

        await communication.DisconnectAsync(CancellationToken.None);

        exchange._subscriptions.Should().ContainSingle();
    }
}
