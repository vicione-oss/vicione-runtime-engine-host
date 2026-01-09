using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.ExternalCommunication;

public class DirectExternalOutgoingCommunication_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
    {
        await using ConstructorProviderFactory providerFactory = new();

        var instance = providerFactory.CreateExternalOutgoing(
            new DirectCommunication(),
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<DirectExternalOutgoingCommunication>();
    }
}

public class DirectExternalOutgoingCommunication_Send
{
    [Fact]
    public async Task Can_send_values_Async()
    {
        using var exchange = new DirectExchange();
        var communication = new DirectExternalOutgoingCommunication(new(), exchange, Substitute.For<ILogger<DirectExternalOutgoingCommunication>>(), Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        var received = new List<ExternalValue>();
        await exchange.SubscribeAsync(["00000000-0000-0000-0000-000000000001", "00000000-0000-0000-0000-000000000002"], new(AssemblyLoadContext.Default.GetHashCode(), _ => throw new InvalidOperationException("Wrong method."), received.Add));

        await communication.SendAsync(8,
            [
                new()
                {
                    Timestamp = new DateTime(2022, 01, 27, 15, 56, 20),
                    Channel = "00000000-0000-0000-0000-000000000001",
                    Validity = 1,
                    Value = 123.45,
                },
                new()
                {
                    Timestamp = new DateTime(2022, 01, 27, 15, 57, 30),
                    Channel = "00000000-0000-0000-0000-000000000002",
                    Validity = 0,
                    Value = "Running...",
                },
            ], CancellationToken.None);

        received.Should().BeEquivalentTo(new List<ExternalValue>
        {
            new()
            {
                Timestamp = new DateTime(2022, 01, 27, 15, 56, 20),
                Channel = "00000000-0000-0000-0000-000000000001",
                Validity = 1,
                Value = 123.45,
            },
            new()
            {
                Timestamp = new DateTime(2022, 01, 27, 15, 57, 30),
                Channel = "00000000-0000-0000-0000-000000000002",
                Validity = 0,
                Value = "Running...",
            },
        });
    }

    [Fact]
    public async Task Logs_error_if_send_fails_Async()
    {
        using DirectExchange exchange = new();
        TestLogger<DirectExternalOutgoingCommunication> logger = new(LogLevel.Information);
        DirectExternalOutgoingCommunication communication = new(new(), exchange, logger, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        await exchange.SubscribeAsync(["A",], new(0, _ => throw new InvalidOperationException("error"), _ => { }));

        await communication.SendAsync(0, [new() { Channel = "A", },], CancellationToken.None);

        logger.Calls.Should().Be(1);
        logger.LogLevel.Should().Be(LogLevel.Error);
        logger.Message.Should().MatchEquivalentOf("*not*send*values*");
        logger.Exception.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("error");
    }
}
