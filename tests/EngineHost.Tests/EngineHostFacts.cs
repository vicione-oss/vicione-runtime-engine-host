using AwesomeAssertions;
using Xunit;

namespace ViciOne.ManagedEngine;

public class EngineHost_VersionWithoutMetadata
{
    [Theory]
    [InlineData("0.1.0", "0.1.0")]
    [InlineData("0.1.0-rc1", "0.1.0-rc1")]
    [InlineData("0.1.0+100", "0.1.0")]
    [InlineData("0.1.0-rc1+100", "0.1.0-rc1")]
    [InlineData(null, null)]
    public void Removes_metdata_from_semver(string? version, string? result)
        => EngineHost.VersionWithoutMetadata(version).Should().Be(result);
}
