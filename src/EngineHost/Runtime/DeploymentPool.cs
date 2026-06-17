using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ViciOne.ManagedEngine.EngineDeployment;

namespace ViciOne.ManagedEngine.Runtime;

internal class DeploymentPool(IOptions<HostConfig> config, IFileSystem fileSystem, JsonSerializerOptionsCache jsonSerializerOptionsCache) : IDeploymentPool
{
    internal readonly ConcurrentDictionary<string, IDeployment> _deployments = new();
    private readonly ConcurrentDictionary<string, int> _startedDeployments = new();
    private readonly HostConfig _config = config.Value;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly JsonSerializerOptionsCache _jsonSerializerOptionsCache = jsonSerializerOptionsCache;

    public bool TryGetDeployment(string id, [NotNullWhen(true)] out IDeployment? deployment)
    {
        var success = _deployments.TryGetValue(id, out var value);
        deployment = value;
        return success;
    }

    public async Task<IDeployment> CreateDeploymentAsync(string deploymentId, AssemblyLoadContext context,
        [StringSyntax(StringSyntaxAttribute.Json)] string startParameter, DeployParameter deployParameter, CancellationToken cancellationToken)
    {
        await DirectoryDeployment.ConfigureDeployment(_config.DeploymentsDirectory, deploymentId, startParameter,
            deployParameter, _fileSystem, _jsonSerializerOptionsCache.GetOrAdd(deploymentId), cancellationToken).ConfigureAwait(false);
        return await CreateAndRegisterAsync(deploymentId, startParameter, context).ConfigureAwait(false);
    }

    public async Task<IDeployment> RecoverDeploymentAsync(string deploymentId, AssemblyLoadContext context, CancellationToken cancellationToken)
    {
        var startParameter = await DirectoryDeployment.ReadStartParameter(_config.DeploymentsDirectory, deploymentId, _fileSystem, cancellationToken).ConfigureAwait(false);
        return await CreateAndRegisterAsync(deploymentId, startParameter, context).ConfigureAwait(false);
    }

    public Task<DeployParameter> GetDeployParameter(string deploymentId, CancellationToken cancellationToken)
        => DirectoryDeployment.ReadDeployParameter(_config.DeploymentsDirectory, deploymentId, _fileSystem, _jsonSerializerOptionsCache.GetOrAdd(deploymentId), cancellationToken);

    private async Task<Deployment> CreateAndRegisterAsync(string deploymentId, [StringSyntax(StringSyntaxAttribute.Json)] string startParameter, AssemblyLoadContext context)
    {
        var deployment = await Deployment.Create(startParameter, context, [.. AssemblyLoadContext.Default.Assemblies]);

        if (!_deployments.TryAdd(deploymentId, deployment))
        {
            await deployment.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"Cannot store deployment '{deploymentId}'.");
        }

        return deployment;
    }

    public async Task RemoveDeploymentAsync(string id)
    {
        if (_deployments.TryRemove(id, out var deployment))
            await deployment.DisposeAsync();
        DirectoryDeployment.Delete(_config.DeploymentsDirectory, id, _fileSystem);
        _jsonSerializerOptionsCache.Remove(id);
    }

    public IReadOnlyCollection<string> GetStartedDeployments()
        => [.. _startedDeployments.Keys];

    public IReadOnlyCollection<string> GetDeployments()
        => [.. _deployments.Keys];

    public void DeploymentStarted(string id)
        => _startedDeployments.TryAdd(id, 0);

    public void DeploymentStopped(string id)
        => _startedDeployments.TryRemove(id, out _);

    public bool DeploymentIsStarted(string id)
        => _startedDeployments.ContainsKey(id);
}
