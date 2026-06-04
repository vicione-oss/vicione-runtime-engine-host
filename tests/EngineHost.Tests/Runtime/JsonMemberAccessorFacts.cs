using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

public class JsonMemberAccessor_ClearCache
{
    [Fact]
    public void Does_not_throw()
        => JsonMemberAccessor.ClearCache();
}
