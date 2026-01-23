using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

public class TransactionContext_
{
    [Fact]
    public async Task Waits_if_transaction_is_pending_Async()
    {
        using TransactionContext transactionContext = new();
        transactionContext.Register(string.Empty);
        var transaction1 = await transactionContext.WaitAsync(string.Empty, 0);
        var transaction2Task = transactionContext.WaitAsync(string.Empty, 3_000);

        await Task.Delay(1000, TestContext.Current.CancellationToken);

        transaction2Task.Status.Should().Be(TaskStatus.WaitingForActivation);

        transaction1.Dispose();
        await transaction2Task;
        transaction2Task.Status.Should().Be(TaskStatus.RanToCompletion);
    }

    [Fact]
    public async Task Throws_if_it_took_to_long_Async()
    {
        using TransactionContext transactionContext = new();
        transactionContext.Register(string.Empty);
        var transaction = await transactionContext.WaitAsync(string.Empty, 0);

        await transactionContext.Awaiting(_ => _.WaitAsync(string.Empty, 1000))
            .Should().ThrowAsync<OperationCanceledException>();

        transaction.Dispose();
    }
}
