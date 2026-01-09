using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions.Testing;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.EngineDeployment;
using ViciOne.ManagedEngine.EngineLog;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.ManagedEngine;

public class AssemblyLoadingFacts
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Empty_deployment_is_unloadable_Async(bool preloadTypeConverterAssemblyInContext)
    {
        WeakReference contextReference;
        StartParameter startParameter = new()
        {
            Engine = new()
            {
                Name = "main",
                UniqueIdentifier = Guid.NewGuid().ToString(),
            },
            EngineLog = new()
            {
                Communication = new() { Id = NullEngineLogCommunication.ID, },
                LogLevel = LogLevel.Warning,
            },
        };

        {
            DirectoryPackageContextResolver resolver = new(string.Empty, preloadTypeConverterAssemblyInContext, new MockFileSystem());
            using NullLoggerFactory loggerFactory = new();
            var context = await resolver.ResolveAsync([], "Test", ContextPool.s_sharedAssemblies, loggerFactory, CancellationToken.None);
            contextReference = new(context);
            await using var deployment = await Deployment.Create(JsonSerializer.Serialize(startParameter, JsonSetup.CreatePreserveTypeOptions()), context, [.. AssemblyLoadContext.Default.Assemblies]);
            {
                await deployment.StartAsync(CancellationToken.None);
                await deployment.StopAsync(CancellationToken.None);
            }

            context.Unload();
        }

        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(10);
        }

        contextReference.IsAlive.Should().BeFalse();
    }
}

[Communication(ID)]
[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Wird von ManagedEngine als public erwartet")]
public sealed class NullEngineLogCommunication : ICommunication
{
    internal const string ID = "nulllog";
}

[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Wird von ManagedEngine als public erwartet")]
public sealed class NullEngineLogFactory : IEngineLogFactory<NullEngineLogCommunication>
{
    public void AddLogging(IServiceCollection services, LogLevel logLevel) { }
    public void ChangeLogLevel(LogLevel logLevel) { }
}
