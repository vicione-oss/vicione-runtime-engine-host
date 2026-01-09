using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

public sealed class TerminateFileObserver_StartAsync : IDisposable
{
    private readonly TemporaryDirectory _directory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IHostEnvironment _environment;

    public TerminateFileObserver_StartAsync()
    {
        _directory = new();
        _lifetime = Substitute.For<IHostApplicationLifetime>();
        _environment = Substitute.For<IHostEnvironment>();
        _environment.ContentRootPath.Returns(_directory.Path);
    }

    public void Dispose() => _directory.Dispose();

    [Fact]
    public async Task Stops_application_on_file_creation()
    {
        using TerminateFileObserver observer = new(_lifetime, _environment, _directory.FileSystem);
        await observer.StartAsync(CancellationToken.None);

        _directory.CreateFile("terminate");

        await Task.Delay(100.Milliseconds());

        _lifetime.Received(2).StopApplication();
    }

    [Fact]
    public async Task Stops_application_on_file_recreation()
    {
        _directory.CreateFile("terminate");
        using TerminateFileObserver observer = new(_lifetime, _environment, _directory.FileSystem);
        await observer.StartAsync(CancellationToken.None);

        _directory.CreateFile("terminate");
        await Task.Delay(200.Milliseconds());

        _lifetime.Received(1).StopApplication();
    }

    [Fact]
    public async Task Does_not_stop_application_if_file_already_exists()
    {
        _directory.CreateFile("terminate");
        using TerminateFileObserver observer = new(_lifetime, _environment, _directory.FileSystem);
        await observer.StartAsync(CancellationToken.None);

        await Task.Delay(100.Milliseconds());

        _lifetime.DidNotReceive().StopApplication();
    }
}

public sealed class TerminateFileObserver_StopAsync : IDisposable
{
    private readonly TemporaryDirectory _directory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IHostEnvironment _environment;

    public TerminateFileObserver_StopAsync()
    {
        _directory = new();
        _lifetime = Substitute.For<IHostApplicationLifetime>();
        _environment = Substitute.For<IHostEnvironment>();
        _environment.ContentRootPath.Returns(_directory.Path);
    }

    public void Dispose() => _directory.Dispose();

    [Fact]
    public async Task Does_not_stop_application_on_file_creation()
    {
        using TerminateFileObserver observer = new(_lifetime, _environment, _directory.FileSystem);
        await observer.StartAsync(CancellationToken.None);
        await observer.StopAsync(CancellationToken.None);

        _directory.CreateFile("terminate");
        await Task.Delay(100.Milliseconds());

        _lifetime.DidNotReceive().StopApplication();
    }

    [Fact]
    public async Task Does_not_stop_application_on_file_recreation()
    {
        _directory.CreateFile("terminate");
        using TerminateFileObserver observer = new(_lifetime, _environment, _directory.FileSystem);
        await observer.StartAsync(CancellationToken.None);
        await observer.StopAsync(CancellationToken.None);

        _directory.CreateFile("terminate");
        await Task.Delay(100.Milliseconds());

        _lifetime.DidNotReceive().StopApplication();
    }
}
