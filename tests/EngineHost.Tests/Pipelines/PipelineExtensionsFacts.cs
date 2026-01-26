using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Testably.Abstractions.Testing;
using ViciOne.ManagedEngine.Pipelines.Requests;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine.Pipelines;

public class PipelineExtensions_
{
    [Fact]
    public async Task Acceptance_Async()
    {
        using LoggerFactory loggerFactory = new();
        TestLogger<int> testLogger = new();
        using TestLoggerProvider loggerProvider = new(testLogger);
        using ManualResetEventSlim resetEvent = new(false);

        using var provider = Given_ServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var context = provider.GetRequiredService<TransactionContext>();

        var id = await When_register_transaction_Async();
        Then_handled_successful();

        var transaction = await When_two_transaction_requests_send_Async();
        Then_both_requests_started();

        await When_transactions_ended_Async(transaction);
        Then_requests_logs_completion();

        await When_unregister_transaction_Async();
        await Then_requests_does_not_wait_Async();

        ServiceProvider Given_ServiceProvider()
        {
            loggerFactory.AddProvider(loggerProvider);

            var serviceCollection = new ServiceCollection()
                .AddMediator()
                .AddSingleton(Substitute.For<IDeploymentPool>())
                .AddSingleton(Substitute.For<IContextPool>())
                .AddSingleton(Substitute.For<IEngineChain>())
                .AddSingleton<TransactionContext>()
                .AddSingleton(resetEvent)
                .AddTransient<IRequestHandler<TestRequest, bool>, TestHandler>()
                .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                .AddSingleton<ILoggerFactory>(loggerFactory)
                .AddSingleton<IFileSystem>(new MockFileSystem());

            serviceCollection.AddOptions<HostConfig>();
            return serviceCollection.BuildServiceProvider();
        }

        Task<string> When_register_transaction_Async()
            => mediator.Send<DeployEngine, string>(new());

        async Task<Task> When_two_transaction_requests_send_Async()
        {
            var task1 = Task.Run(() => mediator.Send<TestRequest, bool>(new() { DeploymentIdentifier = id, }));
            var task2 = Task.Run(() => mediator.Send<TestRequest, bool>(new() { DeploymentIdentifier = id, }));
            await FluentActions.Invoking(() => testLogger.Entries.Count >= 2).WaitForTrue();
            return Task.WhenAll(task1, task2);
        }

        async Task When_transactions_ended_Async(Task transaction)
        {
            resetEvent.Set();
            await transaction;
            resetEvent.Reset();
        }

        Task When_unregister_transaction_Async()
            => mediator.Send<TearDownEngine, bool>(new() { DeploymentIdentifier = id, });

        void Then_handled_successful()
        {
            testLogger.Entries
                .Should().HaveCount(2)
                .And.Contain(e => e.Message.Contains("Handling") || e.Message.Contains("Handled"));
            testLogger.Clear();
        }

        void Then_both_requests_started()
        {
            testLogger.Entries
                .Should().HaveCount(2)
                .And.Contain(e => e.Message.Contains("Handling TestRequest") || !e.Message.Contains("Handled"));
            testLogger.Clear();
        }

        void Then_requests_logs_completion()
        {
            testLogger.Entries
                .Should().HaveCount(2)
                .And.Contain(e => e.Message.Contains("Handled TestRequest") || !e.Message.Contains("Handling"));
            testLogger.Clear();
        }

        async Task Then_requests_does_not_wait_Async()
        {
            var task = Task.Run(async () => { await context.WaitAsync(id, 5_000); resetEvent.Wait(); });
            await context.WaitAsync(id, 5_000);

            resetEvent.Set();
            await task;
        }
    }

    internal sealed class TestRequest : IRequest<bool>
    {
        public string DeploymentIdentifier { get; set; } = string.Empty;
    }

#pragma warning disable CA1812
    internal sealed class TestHandler(ManualResetEventSlim resetEvent, TransactionContext transactionContext)
        : IRequestHandler<TestRequest, bool>
    {
        public async Task<bool> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            using var _ = await transactionContext.WaitAsync(request.DeploymentIdentifier, TransactionContext.Timeout).ConfigureAwait(false);
            resetEvent.Wait(cancellationToken);
            return true;
        }
    }
#pragma warning restore CA1812

    internal sealed class TestLoggerProvider(ILogger logger) : ILoggerProvider
    {
        private readonly ILogger _logger = logger;

        public ILogger CreateLogger(string categoryName) => _logger;

        public void Dispose() { }
    }
}
