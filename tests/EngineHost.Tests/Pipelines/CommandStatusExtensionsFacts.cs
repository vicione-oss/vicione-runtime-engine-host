using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.ManagedEngine.Pipelines;

public class CommandStatusExtensions_OnFailureAsync
{
    [Fact]
    public async Task Executes_function_Async()
        => await Task.FromResult((CommandStatus)new Failure { Message = "Hello World!", })
            .OnFailureAsync(m =>
            {
                m.Should().Be("Hello World!");
                return Task.CompletedTask;
            });
}
