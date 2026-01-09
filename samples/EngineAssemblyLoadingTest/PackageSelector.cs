using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spectre.Console;
using ViciOne.ManagedEngine.PackageResolver;

namespace ViciOne.EngineHost.EngineAssemblyLoadingTest;

internal static class PackageSelector
{
    internal static List<PackageReference> Show(string packagesDirectory)
    {
        var withPreselection = AnsiConsole.Prompt(new SelectionPrompt<bool>()
            .Title("Preselect highest versions?")
            .AddChoices(true, false));

        var multiSelection = new MultiSelectionPrompt<string>()
            .Title("Select packages:")
            .NotRequired();

        Dictionary<string, string[]> availablePackages = [];
        var packageDirectories = Directory.GetDirectories(packagesDirectory);

        foreach (var packageDirectory in packageDirectories)
        {
            var packageName = Path.GetFileName(packageDirectory) ?? string.Empty;
            var versions = Directory.GetDirectories(packageDirectory)
                .Select(d => $"{packageName}@{Path.GetFileName(d) ?? string.Empty}")
                .OrderDescending(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (versions.Length == 0)
                continue;

            multiSelection.AddChoiceGroup(packageName, versions);

            if (withPreselection)
                multiSelection.Select(versions[0]);
        }

        var selection = AnsiConsole.Prompt(multiSelection);
        return [.. selection.Select(PackageReferenceFactory.Parse)];
    }
}
