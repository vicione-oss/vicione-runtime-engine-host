using CsvHelper.Configuration;

namespace Benchmarks.Results;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. Used as a generic parameter and therefore instantiated.
internal sealed class RawResultMap : ClassMap<Result>
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    internal RawResultMap()
    {
        Map(m => m.TotalValues).Name("TotalValues");
        Map(m => m.Links).Name("Links");
        Map(m => m.Technology).Name("Technology");
        Map(m => m.Type).Name("Type");
        Map(m => m.Direction).Name("Direction");
        Map(m => m.Throughput).Name("Throughput [v/s]");
    }
}
