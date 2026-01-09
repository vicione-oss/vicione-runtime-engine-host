using CsvHelper.Configuration;

namespace Benchmarks.Results;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. Used as a generic parameter and therefore instantiated.
internal sealed class RichResultMap : ClassMap<Result>
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    internal RichResultMap()
    {
        Map(m => m.Device).Name(UserColumnNames.Device);
        Map(m => m.Processor).Name(UserColumnNames.Processor);
        Map(m => m.Memory).Name(UserColumnNames.Memory);
        Map(m => m.OperatingSystem).Name(UserColumnNames.OperatingSystem);
        Map(m => m.BenchmarkDotNet).Name(UserColumnNames.BenchmarkDotNet);
        Map(m => m.DotNetSdk).Name(UserColumnNames.DotNetSdk);
        Map(m => m.DotNet).Name(UserColumnNames.DotNet);
        Map(m => m.CommunicationPartner).Name(UserColumnNames.CommunicationPartner);
        Map(m => m.CommunicationPartnerLocation).Name(UserColumnNames.CommunicationPartnerLocation);
        Map(m => m.ConnectionSpeed).Name(UserColumnNames.ConnectionSpeed);
        Map(m => m.TotalValues).Name(UserColumnNames.TotalValues);
        Map(m => m.Links).Name(UserColumnNames.Links);
        Map(m => m.Technology).Name(UserColumnNames.Technology);
        Map(m => m.Protocol).Name(UserColumnNames.Protocol);
        Map(m => m.Type).Name(UserColumnNames.Type);
        Map(m => m.Direction).Name(UserColumnNames.Direction);
        Map(m => m.Throughput).Name(UserColumnNames.Throughput);
    }
}
