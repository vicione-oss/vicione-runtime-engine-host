using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Benchmarks.Communication;

[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Wird mit BenchmarkDotnet als Benchmark-Param genutzt")]
public sealed record class TestCase(float LinkUsage = 1f)
{
    internal TestCase() : this(0f)
    {
    }

    public static IEnumerable<TestCase> DefaultSet()
    {
        yield return new TestCase(0.1f);
        yield return new TestCase(0.2f);
        yield return new TestCase(0.5f);
        yield return new TestCase(1.0f);
    }
}
