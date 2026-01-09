using CsvHelper.Configuration;

namespace Benchmarks.Results;

internal sealed class AdditionalInformationMap : ClassMap<Result>
{
    internal AdditionalInformationMap()
    {
        Map(m => m.Device).Name(UserColumnNames.Device).Constant("Lenovo T14");
        Map(m => m.Processor).Name(UserColumnNames.Processor).Constant("Intel Ultra 7 165H");
        Map(m => m.Memory).Name(UserColumnNames.Memory).Constant("32");
        Map(m => m.OperatingSystem).Name(UserColumnNames.OperatingSystem).Constant("Windows 11 23H2 22631.5189");
        Map(m => m.BenchmarkDotNet).Name(UserColumnNames.BenchmarkDotNet).Constant("0.14.0");
        Map(m => m.DotNetSdk).Name(UserColumnNames.DotNetSdk).Constant("9.0.300");
        Map(m => m.DotNet).Name(UserColumnNames.DotNet).Constant("9.0.5");
        Map(m => m.CommunicationPartner).Name(UserColumnNames.CommunicationPartner).Constant("<Leer>|MQTTnet 4.3.7.1207|HiveMQ 4.40.0|Mosquitto 2.0.21");
        Map(m => m.CommunicationPartnerLocation).Name(UserColumnNames.CommunicationPartnerLocation).Constant("Selber Prozess|Selber Host|Gleiches Netzwerk");
        Map(m => m.ConnectionSpeed).Name(UserColumnNames.ConnectionSpeed).Constant("<Leer>|1000");
        Map(m => m.Protocol).Name(UserColumnNames.Protocol).Constant("<Leer>|TCP|WebSocket");
    }
}
