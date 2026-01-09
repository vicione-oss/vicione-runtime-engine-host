using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Generation;
using ViciOne.Core.Dataflow.DataModel.Generation.IntermediateModel;
using ViciOne.ManagedEngine;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.EngineDeployment;
using ViciOne.ManagedEngine.PackageResolver;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.EngineHost.EngineAssemblyLoadingTest;

internal static class AssemblyLoadingTest
{
    internal static async Task RunAsync(string packagesDirectory, IReadOnlyCollection<PackageReference> packages, string? designFilter, IFileSystem fileSystem)
    {
        var contextReference = await RunEngineAsync(packagesDirectory, packages, designFilter, fileSystem);

        await TryClearGcAsync();
        await PrintAliveStateAsync(contextReference);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> RunEngineAsync(string packagesDirectory, IReadOnlyCollection<PackageReference> packageReferences, string? designFilter, IFileSystem fileSystem)
    {
        DirectoryPackageContextResolver resolver = new(packagesDirectory, true, fileSystem);
        using NullLoggerFactory loggerFactory = new();
        var context = await resolver.ResolveAsync(packageReferences, "main", ContextPool.s_sharedAssemblies, loggerFactory, CancellationToken.None);
        var fbDesignInfos = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesigns(context.Assemblies)
            .Where(d => designFilter is null || d.Name.Contains(designFilter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        StartParameter startParameter = new()
        {
            Engine = new()
            {
                Name = "main",
                UniqueIdentifier = Guid.NewGuid().ToString(),
            },
            EngineLog = new()
            {
                Communication = new() { Id = ConsoleEngineLogCommunication.ID, },
                LogLevel = LogLevel.Debug,
            },
            Dataflow =
            {
                FunctionBlocks = [.. fbDesignInfos.Select(CreateFunctionBlock)],
            },
        };

        await using var deployment = await Deployment.Create(JsonSerializer.Serialize(startParameter, JsonSetup.CreatePreserveTypeOptions()), context, [typeof(ConsoleEngineLogCommunication).Assembly]);
        {
            await deployment.StartAsync(CancellationToken.None);

            for (var i = 0; i < 10; i++)
            {
                deployment.ProcessCycle();
                await Task.Delay(100);
            }

            await deployment.StopAsync(CancellationToken.None);
        }

        context.Unload();

        return new(context);
    }

    private static FunctionBlock CreateFunctionBlock(FunctionBlockDesignInfo fbDesignInfo)
        => new()
        {
            UniqueIdentifier = Guid.NewGuid().ToString(),
            RunMode = (fbDesignInfo.ValidRunModes ?? []).Contains(RunMode.Cyclic) ? FunctionBlockRunMode.Cycle : FunctionBlockRunMode.Change,
            CycleFrequency = fbDesignInfo.DefaultCycleFrequency ?? 1,
            Name = $"{fbDesignInfo.Namespace}.{fbDesignInfo.Name}",
            DesignIdentifier = fbDesignInfo.Id.ToString(),
            Connectors =
            [
                .. fbDesignInfo.Inputs.Select(i => new FunctionBlockConnector()
                {
                    UniqueIdentifier = Guid.NewGuid().ToString(),
                    DesignIdentifier = i.Id.ToString(),
                    InitialValue = i.DefaultValue ?? i.Type.GetDefaultValue(),
                }),
                .. fbDesignInfo.Outputs.Select(o => new FunctionBlockConnector()
                {
                    UniqueIdentifier = Guid.NewGuid().ToString(),
                    DesignIdentifier = o.Id.ToString(),
                    InitialValue = o.DefaultValue ?? o.Type.GetDefaultValue(),
                }),
            ],
            Variables = [.. fbDesignInfo.PersistentVariables.Select(v => new FunctionBlockVariable()
            {
                UniqueIdentifier = Guid.NewGuid().ToString(),
                DesignIdentifier = v.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                Value = v.DefaultValue ?? v.Type.GetDefaultValue(),
            })],
            Settings = [.. fbDesignInfo.Settings.Select(s => new FunctionBlockSetting()
            {
                UniqueIdentifier = Guid.NewGuid().ToString(),
                DesignIdentifier = s.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                Value = s.DefaultValue ?? s.Type.GetDefaultValue(),
            })],
        };

    private static object? GetDefaultValue(this Type type)
    {
        if (type.IsValueType)
            return RuntimeHelpers.GetUninitializedObject(type);
        return null;
    }

    private static async Task TryClearGcAsync()
    {
        await Task.Delay(1_000);

        for (var i = 0; i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(50);
        }
    }

    private static async Task PrintAliveStateAsync(WeakReference contextReference)
    {
        if (contextReference.IsAlive)
        {
            Console.WriteLine("...");
            await Task.Delay(10_000);
            PrintAliveState(contextReference.IsAlive);
        }
        else
        {
            PrintAliveState(false);
        }

        static void PrintAliveState(bool isAlive)
        {
            if (isAlive)
                Console.WriteLine("Context is alive.");
            else
                Console.WriteLine("Context is dead.");
        }
    }
}
