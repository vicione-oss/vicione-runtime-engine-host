using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.EngineDeployment;

namespace ViciOne.ManagedEngine;

internal static class UI
{
    internal static AppSettings ConfigureApp(CommandLineParameters parameters, IReadOnlyCollection<IDeployParameterFactory> deployParameterFactories)
    {
        var factory = ChooseDeployParameter(parameters.DeployParameter, deployParameterFactories);
        return new(factory, parameters.EngineHostUniqueIdentifier, parameters.MqttHost, parameters.MqttPort, parameters.MqttBroker);
    }

    private static DeployParameter[] ChooseDeployParameter(string? deployParameter, IReadOnlyCollection<IDeployParameterFactory> deployParameterFactories)
    {
        var metaInfo = CollectMetaInfo(deployParameterFactories);
        var candidate = metaInfo.FirstOrDefault(_ => _.Moniker == deployParameter);
        if (candidate is null)
        {
            var index = -1;
            while (index < 0 || index >= metaInfo.Count)
            {
                ShowFactories(metaInfo);
                index = SelectListIndex();
            }
            candidate = metaInfo[index];
        }
        return candidate.Factory.CreateDeployParameters();
    }

    private static List<FactoryMetaInfo> CollectMetaInfo(IReadOnlyCollection<IDeployParameterFactory> deployParameterFactories)
        => [.. deployParameterFactories.Select(CollectMetaInfo).OrderBy(_ => _.Moniker)];

    private static FactoryMetaInfo CollectMetaInfo(IDeployParameterFactory factory)
    {
        var type = factory.GetType();
        var moniker = new string([.. type.Name.Where(char.IsUpper)]);
        var description = string.Empty;
        var descriptionAttribute = type.GetCustomAttribute<DescriptionAttribute>();
        if (descriptionAttribute != null)
            description = descriptionAttribute.Description;
        return new(moniker, description, factory);
    }

    private static void ShowFactories(IEnumerable<FactoryMetaInfo> factories)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Deploy parameter:");
        Console.ResetColor();
        var i = 0;
        foreach (var factory in factories)
            Console.WriteLine($"    {i++} = {factory.Moniker} {factory.Description}");
    }

    private static int SelectListIndex()
    {
        var line = Console.ReadLine();
        if (!int.TryParse(line, out var integer))
            integer = -1;
        return integer;
    }

    internal static async Task PrintStatusAsync(this Task<EngineState> statusTask, string deployment)
    {
        var status = await statusTask;
        PrintDeploymentAction(deployment, status.ToString(), ConsoleColor.Blue);
    }

    internal static async Task<string> PrintAsync(this Task<string> task, string text)
    {
        var result = await task;
        PrintDeploymentAction(result, text, ConsoleColor.DarkYellow);

        return result;
    }

    internal static async Task PrintAsync(this Task task, string text, string deployment)
    {
        PrintDeploymentAction(deployment, text, ConsoleColor.DarkYellow);

        await task;
    }

    private static void PrintDeploymentAction(string deployment, string action, ConsoleColor color)
    {
        Console.Write($"{deployment}: ");
        Console.ForegroundColor = color;
        Console.WriteLine(action);
        Console.ResetColor();
    }

    internal static async Task WaitAsync(int milliseconds)
    {
        var watch = Stopwatch.StartNew();

        do
        {
            PrintStaticText($"{watch.Elapsed.TotalSeconds:0}s");
            await Task.Delay(250);
        }
        while (watch.Elapsed.TotalMilliseconds <= milliseconds);

        watch.Stop();
        Console.WriteLine();
    }

    private static void PrintStaticText(string text)
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(text);
    }

    private sealed record class FactoryMetaInfo(string Moniker, string Description, IDeployParameterFactory Factory);
}
