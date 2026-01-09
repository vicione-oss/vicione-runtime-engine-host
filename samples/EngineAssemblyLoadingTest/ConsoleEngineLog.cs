using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.EngineLog;

namespace ViciOne.EngineHost.EngineAssemblyLoadingTest;

/// <summary>
/// ConsoleEngineLogCommunication
/// </summary>
[Communication(ID)]
[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Wird von ManagedEngine als public erwartet")]
public sealed class ConsoleEngineLogCommunication : ICommunication
{
    internal const string ID = "console";
}

/// <summary>
/// ConsoleEngineLogFactory
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Wird von ManagedEngine als public erwartet")]
public sealed class ConsoleEngineLogFactory : IEngineLogFactory<ConsoleEngineLogCommunication>
{
    /// <summary>
    /// AddLogging
    /// </summary>
    public void AddLogging(IServiceCollection services, LogLevel logLevel)
        => services.AddLogging(c => c.AddConsole().SetMinimumLevel(logLevel));

    /// <summary>
    /// ChangeLogLevel
    /// </summary>
    public void ChangeLogLevel(LogLevel logLevel) { }
}
