using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ViciOne.ManagedEngine.Runtime;

internal sealed class TerminateFileObserver : IHostedService, IDisposable
{
    private readonly IFileSystemWatcher _watcher;
    private readonly IHostApplicationLifetime _lifetime;

    public TerminateFileObserver(IHostApplicationLifetime lifetime, IHostEnvironment environment, IFileSystem fileSystem)
    {
        _watcher = fileSystem.FileSystemWatcher.New(environment.ContentRootPath, "terminate");
        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        _watcher.Created += TerminateFileCreatedOrRecreated;
        _watcher.Changed += TerminateFileCreatedOrRecreated;
        _lifetime = lifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _watcher.EnableRaisingEvents = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher.EnableRaisingEvents = false;
        return Task.CompletedTask;
    }

    private void TerminateFileCreatedOrRecreated(object sender, FileSystemEventArgs e)
        => _lifetime.StopApplication();

    public void Dispose()
    {
        _watcher.Created -= TerminateFileCreatedOrRecreated;
        _watcher.Changed -= TerminateFileCreatedOrRecreated;
        _watcher.Dispose();
    }
}
