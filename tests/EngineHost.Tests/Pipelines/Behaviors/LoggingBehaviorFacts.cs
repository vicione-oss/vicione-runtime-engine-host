using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ViciOne.ManagedEngine.Pipelines.Behaviors;

public class LoggingBehavior_
{
    [Fact]
    public async Task Logs_good_path()
    {
        TestLogger<LoggingBehavior<TestRequest, bool>> logger = new();
        var behavior = new LoggingBehavior<TestRequest, bool>(logger);

        var act = behavior.Awaiting(x => x.Handle(new(), new(c => Task.FromResult(true)), CancellationToken.None));

        await act.Should().NotThrowAsync();
        logger.Entries.Should().SatisfyRespectively(
            e =>
            {
                e.LogLevel.Should().Be(LogLevel.Debug);
                e.Message.Should().MatchEquivalentOf("*handling*TestRequest*");
            },
            e =>
            {
                e.LogLevel.Should().Be(LogLevel.Debug);
                e.Message.Should().MatchEquivalentOf("*handled*TestRequest*");
            });
    }

    [Fact]
    public async Task Logs_bad_path()
    {
        TestLogger<LoggingBehavior<TestRequest, bool>> logger = new();
        var behavior = new LoggingBehavior<TestRequest, bool>(logger);

        var act = behavior.Awaiting(x => x.Handle(new(), new(c => Task.FromException<bool>(new InvalidOperationException("Abbruchbedingung"))), CancellationToken.None));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Abbruchbedingung");
        logger.Entries.Should().SatisfyRespectively(
            e =>
            {
                e.LogLevel.Should().Be(LogLevel.Debug);
                e.Message.Should().MatchEquivalentOf("*handling*TestRequest*");
            },
            e =>
            {
                e.LogLevel.Should().Be(LogLevel.Error);
                e.Message.Should().MatchEquivalentOf("*handling*TestRequest*failed*");
            });
    }

    internal sealed class TestRequest : IRequest<bool> { }
}
