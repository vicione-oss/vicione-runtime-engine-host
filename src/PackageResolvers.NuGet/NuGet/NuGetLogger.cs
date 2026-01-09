using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using MicrosoftLogger = Microsoft.Extensions.Logging.ILogger;

namespace ViciOne.ManagedEngine.PackageResolver.NuGet;

[SuppressMessage("Usage", "CA2254:Vorlage muss ein statischer Ausdruck sein", Justification = "Meldung wird nur weitergeleitet")]
[SuppressMessage("Performance", "CA1848:LoggerMessage-Delegaten verwenden", Justification = "Meldung wird nur weitergeleitet")]
internal sealed class NuGetLogger(MicrosoftLogger logger) : LegacyLoggerAdapter
{
    private readonly MicrosoftLogger _logger = logger;

    public override void LogDebug(string data)
        => _logger.LogDebug(data);

    public override void LogError(string data)
        => _logger.LogError(data);

    public override void LogInformation(string data)
        => _logger.LogInformation(data);

    public override void LogInformationSummary(string data)
        => _logger.LogInformation(data);

    public override void LogMinimal(string data)
        => _logger.LogInformation(data);

    public override void LogVerbose(string data)
        => _logger.LogTrace(data);

    public override void LogWarning(string data)
        => _logger.LogWarning(data);
}
