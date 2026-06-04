using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

public class JsonSerializerOptionsAccessor_ClearCaches
{
    [Fact]
    public void Options_remain_usable_after_clear()
    {
        JsonSerializerOptions options = new();
        JsonSerializer.Serialize(new { A = 1 }, options);

        options.ClearCaches();

        var result = JsonSerializer.Serialize(new { A = 2 }, options);
        result.Should().Be("""{"A":2}""");
    }
}
