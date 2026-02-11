using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.PackageResolver;

/// <summary>
/// Resolves NuGet packages using the .NET SDK tooling.
/// </summary>
/// <param name="outputDirectory">The output directory for resolved packages.</param>
/// <param name="fileSystem">The file system abstraction.</param>
/// <param name="packageSources">The NuGet package sources to use.</param>
/// <param name="logger">The logger instance.</param>
public sealed class DotNetSdkPackageResolver(string outputDirectory, IFileSystem fileSystem, IReadOnlyCollection<NuGetPackageSource>? packageSources = null, ILogger<DotNetSdkPackageResolver>? logger = null) : IPackageResolver
{
    private readonly IReadOnlyCollection<NuGetPackageSource>? _packageSources = packageSources;
    private readonly string _outputDirectory = outputDirectory;
    private readonly ILogger<DotNetSdkPackageResolver>? _logger = logger;
    private readonly IFileSystem _fileSystem = fileSystem;
    private const string TFM = "net10.0";

    /// <summary>
    /// Initializes a new instance of the <see cref="DotNetSdkPackageResolver"/> class with a single package source.
    /// </summary>
    /// <param name="outputDirectory">The output directory for resolved packages.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="packageSource">The NuGet package source to use.</param>
    /// <param name="logger">The logger instance.</param>
    public DotNetSdkPackageResolver(string outputDirectory, IFileSystem fileSystem, NuGetPackageSource packageSource, ILogger<DotNetSdkPackageResolver>? logger = null)
        : this(outputDirectory, fileSystem, [packageSource,], logger)
    { }

    /// <inheritdoc />
    public async Task<ResolveResult> ResolveAsync(IReadOnlyCollection<PackageReference> packageReferences, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var projectDirectory = CreateProjectDirectory();
            try
            {
                var projectFilePath = await CreateProjectFileAsync(projectDirectory, packageReferences).ConfigureAwait(false);
                await CreateNuGetConfigAsync(projectDirectory, _packageSources).ConfigureAwait(false);
                await PublishProject(projectFilePath, _outputDirectory, cancellationToken);
                return new([.. CreatePackages(_outputDirectory, _fileSystem)], new(await ReadDependencyFileAsync(_outputDirectory, _fileSystem).ConfigureAwait(false)));
            }
            finally
            {
                RemoveProjectDirectory(projectDirectory);
            }
        }
        catch (Exception ex)
        {
            var sourcesListing = new StringBuilder("system sources");
            if (_packageSources is not null)
                sourcesListing.AppendJoin(", ", _packageSources.Select(s => $"'{s}'"));
            throw new InvalidOperationException($"Could not resolve packages from {sourcesListing}.", ex);
        }
    }

    private string CreateProjectDirectory()
    {
        var projectDirectory = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), _fileSystem.Path.GetRandomFileName()));
        _fileSystem.Directory.CreateDirectory(projectDirectory);
        _logger?.DirectoryCreated(projectDirectory);
        return projectDirectory;
    }

    private async Task<string> CreateProjectFileAsync(string projectDirectory, IReadOnlyCollection<PackageReference> packageReferences)
    {
        var projectFilePath = _fileSystem.Path.Combine(projectDirectory, "meta.csproj");
        await using var stream = _fileSystem.File.OpenWrite(projectFilePath);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>").ConfigureAwait(false);
        await writer.WriteAsync(TFM).ConfigureAwait(false);
        await writer.WriteLineAsync("</TargetFramework><AssemblyName>meta</AssemblyName><IsPublishable>true</IsPublishable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup><ItemGroup>").ConfigureAwait(false);
        foreach (var packageReference in packageReferences)
        {
            await writer.WriteAsync("<PackageReference Include=\"").ConfigureAwait(false);
            await writer.WriteAsync(packageReference.Name).ConfigureAwait(false);
            await writer.WriteAsync("\" Version=\"").ConfigureAwait(false);
            await writer.WriteAsync(packageReference.Version).ConfigureAwait(false);
            await writer.WriteLineAsync("\" />").ConfigureAwait(false);
        }
        await writer.WriteLineAsync("</ItemGroup></Project>").ConfigureAwait(false);
        _logger?.ProjectCreated(packageReferences.Count);
        return projectFilePath;
    }

    private async Task CreateNuGetConfigAsync(string directory, IReadOnlyCollection<NuGetPackageSource>? packageSources)
    {
        await using var stream = _fileSystem.File.OpenWrite(_fileSystem.Path.Combine(directory, "NuGet.Config"));
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync("<?xml version=\"1.0\" encoding=\"utf-8\"?><configuration>").ConfigureAwait(false);
        if (packageSources is not null)
        {
            var packageSourceIndex = 0;
            var packageSourceNames = packageSources.ToDictionary(s => s, s => $"Source{packageSourceIndex++}");
            foreach (var packageSource in packageSources)
            {
                await writer.WriteAsync("<add key=\"").ConfigureAwait(false);
                await writer.WriteAsync(packageSourceNames[packageSource]).ConfigureAwait(false);
                await writer.WriteAsync("\" value=\"").ConfigureAwait(false);
                await writer.WriteAsync(packageSource.Source).ConfigureAwait(false);
                await writer.WriteLineAsync("\" protocolVersion=\"3\" />").ConfigureAwait(false);
            }
            await writer.WriteLineAsync("</packageSources><packageSourceCredentials>").ConfigureAwait(false);
            foreach (var packageSource in packageSources.Where(s => s.Credentials is not null))
            {
                await writer.WriteAsync("<").ConfigureAwait(false);
                await writer.WriteAsync(packageSourceNames[packageSource]).ConfigureAwait(false);
                await writer.WriteAsync(">").ConfigureAwait(false);

                switch (packageSource.Credentials)
                {
                    case NuGetBasicAuthentication basic:
                        await writer.WriteAsync("<add key=\"Username\" value=\"").ConfigureAwait(false);
                        await writer.WriteAsync(basic.Username).ConfigureAwait(false);
                        await writer.WriteAsync("\" /><add key=\"").ConfigureAwait(false);
                        if (basic.PasswordIsClearText)
                            await writer.WriteAsync("ClearText").ConfigureAwait(false);
                        await writer.WriteAsync("Password\" value=\"").ConfigureAwait(false);
                        await writer.WriteAsync(basic.Password).ConfigureAwait(false);
                        await writer.WriteLineAsync("\" />").ConfigureAwait(false);
                        break;
                }
                await writer.WriteAsync("</").ConfigureAwait(false);
                await writer.WriteAsync(packageSourceNames[packageSource]).ConfigureAwait(false);
                await writer.WriteLineAsync(">").ConfigureAwait(false);
            }
            await writer.WriteLineAsync("</packageSourceCredentials>").ConfigureAwait(false);
        }
        await writer.WriteLineAsync("</configuration>").ConfigureAwait(false);
        _logger?.NuGetConfigCreated(packageSources?.Count ?? 0);
    }

    private async Task PublishProject(string projectFilePath, string outputDirectory, CancellationToken cancellationToken)
    {
        var arguments = new StringBuilder("publish --nologo -o ")
            .Append(outputDirectory)
            .Append(' ')
            .Append(projectFilePath);
        using Process process = new();
        var executable = "dotnet";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            executable += ".exe";
        ProcessStartInfo processStartInfo = new()
        {
            FileName = executable,
            Arguments = arguments.ToString(),
            WorkingDirectory = Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };
        processStartInfo.Environment.Add("DOTNET_CLI_UI_LANGUAGE", "en-US");
        process.StartInfo = processStartInfo;
        StringBuilder processOutput = new();
        process.OutputDataReceived += (_, e) => processOutput.Append(e.Data);
        process.Start();
        process.BeginOutputReadLine();
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
            throw new InvalidOperationException(processOutput.ToString());
        _logger?.ProjectPublished();
    }

    private void RemoveProjectDirectory(string projectDirectory)
    {
        _fileSystem.Directory.Delete(projectDirectory, true);
        _logger?.DirectoryRemoved();
    }

    private static Package[] CreatePackages(string directory, IFileSystem system)
    {
        var assets = system.Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(e => new Asset(e))
            .ToList();
        return [new(directory, assets)];
    }

    private static async Task<string> ReadDependencyFileAsync(string outputDir, IFileSystem system)
    {
        var content = string.Empty;
        var depsJsonFile = system.Directory.EnumerateFileSystemEntries(outputDir, "*.deps.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (depsJsonFile is not null)
            content = await system.File.ReadAllTextAsync(depsJsonFile).ConfigureAwait(false);
        return content;
    }
}
