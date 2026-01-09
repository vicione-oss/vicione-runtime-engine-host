using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using ViciOne.ManagedEngine.PackageResolver;
using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

public class PackageReferenceIgnoreCasingEqualityComparer_Equals
{
    private readonly PackageReferenceIgnoreCasingEqualityComparer _comparer = new();

    [Theory]
    [MemberData(nameof(PackageReferenceIgnoreCasingEqualityComparerFacts.GetEqualData), MemberType = typeof(PackageReferenceIgnoreCasingEqualityComparerFacts))]
    [MemberData(nameof(PackageReferenceIgnoreCasingEqualityComparerFacts.GetNullableEqualData), MemberType = typeof(PackageReferenceIgnoreCasingEqualityComparerFacts))]
    [SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable",
        Justification = "Xunit Serialisierung könnte die Tests beeinflussen.")]
    public void IsEqual(PackageReference? a, PackageReference? b)
        => _comparer.Equals(a, b).Should().BeTrue();

    [Theory]
    [MemberData(nameof(PackageReferenceIgnoreCasingEqualityComparerFacts.GetUnequalData), MemberType = typeof(PackageReferenceIgnoreCasingEqualityComparerFacts))]
    [MemberData(nameof(PackageReferenceIgnoreCasingEqualityComparerFacts.GetNullableUnequalData), MemberType = typeof(PackageReferenceIgnoreCasingEqualityComparerFacts))]
    [SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable",
        Justification = "Xunit Serialisierung könnte die Tests beeinflussen.")]
    public void IsUnequal(PackageReference? a, PackageReference? b)
        => _comparer.Equals(a, b).Should().BeFalse();
}

public class PackageReferenceIgnoreCasingEqualityComparer_GetHashCode
{
    private readonly PackageReferenceIgnoreCasingEqualityComparer _comparer = new();

    [Theory]
    [MemberData(nameof(PackageReferenceIgnoreCasingEqualityComparerFacts.GetEqualData), MemberType = typeof(PackageReferenceIgnoreCasingEqualityComparerFacts))]
    [SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable",
        Justification = "Xunit Serialisierung könnte die Tests beeinflussen.")]
    public void IsEqual(PackageReference? a, PackageReference? b)
        => _comparer.GetHashCode(a!).Should().Be(_comparer.GetHashCode(b!));

    [Theory]
    [MemberData(nameof(PackageReferenceIgnoreCasingEqualityComparerFacts.GetUnequalData), MemberType = typeof(PackageReferenceIgnoreCasingEqualityComparerFacts))]
    [SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable",
        Justification = "Xunit Serialisierung könnte die Tests beeinflussen.")]
    public void IsUnequal(PackageReference? a, PackageReference? b)
        => _comparer.GetHashCode(a!).Should().NotBe(_comparer.GetHashCode(b!));
}

internal static class PackageReferenceIgnoreCasingEqualityComparerFacts
{

    public static TheoryData<PackageReference?, PackageReference?> GetEqualData()
        => new()
        {
            { new PackageReference { Name = "Package", Version = "1.0.0", }, new PackageReference { Name = "Package", Version = "1.0.0", } },
            { new PackageReference { Name = "Package", Version = "1.0.0", }, new PackageReference { Name = "package", Version = "1.0.0", } },
            { new PackageReference { Name = "Package", Version = "1.0.0-ci", }, new PackageReference { Name = "Package", Version = "1.0.0-CI", } },
        };
    public static TheoryData<PackageReference?, PackageReference?> GetNullableEqualData()
        => new()
        {
            { null, null },
        };

    public static TheoryData<PackageReference?, PackageReference?> GetUnequalData()
        => new()
        {
            { new PackageReference { Name = "Package", Version = "1.0.0", }, new PackageReference { Name = "Different", Version = "1.0.0", } },
            { new PackageReference { Name = "Package", Version = "1.0.0", }, new PackageReference { Name = "Package", Version = "2.0.0", } },
        };

    public static TheoryData<PackageReference?, PackageReference?> GetNullableUnequalData()
        => new()
        {
            { null, new PackageReference() },
            { new PackageReference(), null },
        };
}
