using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Pipelines;
using ViciOne.ManagedEngine.Pipelines.Commands;
using ViciOne.ManagedEngine.Pipelines.Queries;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine.EngineDeployment;

internal sealed class Deployment : IDeployment, IAsyncDisposable
{
    private readonly ManagedEngine _managedEngine;
    private readonly ConstructorProviderFactory _providerFactory;

    private Deployment(ManagedEngine managedEngine, ConstructorProviderFactory providerFactory)
    {
        _managedEngine = managedEngine;
        _providerFactory = providerFactory;
    }

    public static async Task<Deployment> Create([StringSyntax(StringSyntaxAttribute.Json)] string startParameter,
        AssemblyLoadContext context, IReadOnlyCollection<Assembly>? additionalAssemblies = null)
    {
        additionalAssemblies ??= [];
        ManagedEngine? managedEngine = default;
        ConstructorProviderFactory? providerFactory = default;

        try
        {
            var options = JsonSetup.CreatePreserveTypeOptions(context);
            var startParameterInstance = JsonSerializer.Deserialize<StartParameter>(startParameter, options)
                ?? throw new InvalidOperationException("Cannot deserialize start parameter.");
#pragma warning disable CA2000 // Dispose objects before losing scope. Responsibility is transferred to another class.
            providerFactory = new([.. context.Assemblies, .. additionalAssemblies]);
            managedEngine = ManagedEngine.Create(startParameterInstance, context, providerFactory);
#pragma warning restore CA2000 // Dispose objects before losing scope

            return new(managedEngine, providerFactory);
        }
        catch (Exception)
        {
            if (managedEngine is not null)
                await managedEngine.DisposeAsync();
            if (providerFactory is not null)
                await providerFactory.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
        => await _managedEngine.Handle<StartEngineCommand, CommandStatus>(new(), cancellationToken)
            .OnFailureAsync(async error =>
            {
                var startException = new InvalidOperationException(error);
                try
                {
                    await StopAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new AggregateException(startException, ex);
                }
                throw startException;
            })
            .ConfigureAwait(false);

    public async Task StopAsync(CancellationToken cancellationToken)
        => await _managedEngine.Handle<StopEngineCommand, CommandStatus>(new(), cancellationToken)
            .OnFailureAsync(error => throw new InvalidOperationException(error))
            .ConfigureAwait(false);

    public Task<EngineState> GetStateAsync(CancellationToken cancellationToken)
        => _managedEngine.Handle<EngineStateQuery, EngineState>(new(), cancellationToken);

    public ulong ProcessCycle()
        => _managedEngine.ProcessCycle();

    public async ValueTask DisposeAsync()
    {
        await _managedEngine.DisposeAsync();
        await _providerFactory.DisposeAsync().ConfigureAwait(false);
    }
}
