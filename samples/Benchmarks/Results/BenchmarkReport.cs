using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace Benchmarks.Results;

internal static class BenchmarkReport
{
    internal static Task EnrichAsync(string csvReportFilePath)
        => WriteExceptionToErrorStreamAsync(async () =>
        {
            var additionalInformation = await RequestAdditionalInformationAsync();
            var rawReport = ReadRawCsvReport(csvReportFilePath);
            var richReport = EnrichReport(rawReport, additionalInformation);
            WriteRichCsvReport(csvReportFilePath, richReport);
            await Task.CompletedTask;
        });

    private static async Task WriteExceptionToErrorStreamAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);
        }
    }

    private static async Task<Result> RequestAdditionalInformationAsync()
    {
        var additionalInformationFilePath = string.Concat(Path.GetTempFileName(), ".txt");
        AdditionalInformationMap map = new();
        const string Separator = "=";
        using (StreamWriter writer = new(additionalInformationFilePath))
        {
            foreach (var member in map.MemberMaps)
            {
                await writer.WriteAsync(member.Data.Names.First());
                await writer.WriteAsync(Separator);
                await writer.WriteLineAsync(member.Data.Constant?.ToString());
            }
        }
        ProcessStartInfo editorStartInfo = new(additionalInformationFilePath)
        {
            UseShellExecute = true,
        };
        using var editor = Process.Start(editorStartInfo);
        if (editor is not null)
            await editor.WaitForExitAsync();
        Dictionary<string, string> inputs = [];
        using (StreamReader reader = new(additionalInformationFilePath))
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var index = line.IndexOf(Separator, StringComparison.Ordinal);
                    if (index >= 0)
                        inputs.Add(line[..index], line[(index + 1)..]);
                }
            }
        }
        Result additionalInformation = new();
        foreach (var member in map.MemberMaps)
        {
            object value = inputs[member.Data.Names.First()];
            ((PropertyInfo?)member.Data.Member)?.SetValue(additionalInformation, value);
        }
        return additionalInformation;
    }

    private static List<Result> ReadRawCsvReport(string filePath)
    {
        using StreamReader reader = new(filePath);
        CsvConfiguration config = new(CultureInfo.InvariantCulture);
        using CsvReader csv = new(reader, config);
        csv.Context.RegisterClassMap<RawResultMap>();
        var records = csv.GetRecords<Result>();
        return [.. records];
    }

    private static IReadOnlyCollection<Result> EnrichReport(IReadOnlyCollection<Result> report, Result additionalInformation)
    {
        foreach (var result in report)
        {
            result.Device = additionalInformation.Device;
            result.Processor = additionalInformation.Processor;
            result.Memory = additionalInformation.Memory;
            result.OperatingSystem = additionalInformation.OperatingSystem;
            result.BenchmarkDotNet = additionalInformation.BenchmarkDotNet;
            result.DotNetSdk = additionalInformation.DotNetSdk;
            result.DotNet = additionalInformation.DotNet;
            result.CommunicationPartnerLocation = additionalInformation.CommunicationPartnerLocation;
            result.CommunicationPartner = additionalInformation.CommunicationPartner;
            result.ConnectionSpeed = additionalInformation.ConnectionSpeed;
            result.Protocol = additionalInformation.Protocol;
        }
        return report;
    }

    private static void WriteRichCsvReport(string filePath, IReadOnlyCollection<Result> records)
    {
        using StreamWriter writer = new(filePath);
        CsvConfiguration config = new(CultureInfo.InvariantCulture);
        using CsvWriter csv = new(writer, config);
        csv.Context.RegisterClassMap<RichResultMap>();
        csv.WriteRecords(records);
    }
}
