using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViciOne.ManagedEngine.EngineDeployment;
using ViciOne.ManagedEngine.Pipelines;
using ViciOne.ManagedEngine.Pipelines.Requests;

namespace ViciOne.ManagedEngine.Runtime;

internal class DeploymentsService(
    IOptions<HostConfig> options,
    IDeploymentPool deploymentPool,
    IContextPool contextPool,
    IEngineChain engineChain,
    IMediator mediator,
    ILogger<DeploymentsService> logger,
    IFileSystem fileSystem) : IHostedLifecycleService
{
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly HostConfig _config = options.Value;
    private readonly IDeploymentPool _deploymentPool = deploymentPool;
    private readonly IContextPool _contextPool = contextPool;
    private readonly IEngineChain _engineChain = engineChain;
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<DeploymentsService> _logger = logger;

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        EnsureDeploymentDirectory();
        return Task.CompletedTask;
    }

    private void EnsureDeploymentDirectory() => _fileSystem.Directory.CreateDirectory(_config.DeploymentsDirectory);

    public Task StartAsync(CancellationToken cancellationToken) => RecoverDeployments(cancellationToken);

    private async Task RecoverDeployments(CancellationToken cancellationToken)
    {
        var deploymentIdentifiers = DirectoryDeployment.GetDeployments(_config.DeploymentsDirectory, _fileSystem);
        var recoveredDeployments = 0;
        foreach (var deploymentIdentifier in deploymentIdentifiers)
        {
            try
            {
                var chainIndex = await RecoverDeployment(deploymentIdentifier, cancellationToken).ConfigureAwait(false);
                recoveredDeployments++;
                _logger.RecoveredDeployment(deploymentIdentifier, chainIndex);
            }
            catch (Exception ex)
            {
                _logger.RecoverDeploymentFailed(deploymentIdentifier, ex);
            }
        }
        _logger.RecoveredDeployments(recoveredDeployments, deploymentIdentifiers.Count - recoveredDeployments, deploymentIdentifiers.Count);
    }

    private async Task<int> RecoverDeployment(string deploymentIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            var deployParameter = await DirectoryDeployment.ReadDeployParameter(_config.DeploymentsDirectory, deploymentIdentifier, _fileSystem, cancellationToken).ConfigureAwait(false);
            var context = await _contextPool.RegisterDeploymentAsync(deploymentIdentifier, deployParameter.PackageReferences,
                cancellationToken).ConfigureAwait(false);
            var deployment = await _deploymentPool.RecoverDeploymentAsync(deploymentIdentifier, context, cancellationToken).ConfigureAwait(false);

            await _engineChain.SetUpCycleTimeAsync(TimeSpan.FromMilliseconds(deployParameter.CycleTime)).ConfigureAwait(false);
            _engineChain.AddChainLink(deploymentIdentifier, deployment.ProcessCycle, deployParameter.EngineChainIndex);
            return deployParameter.EngineChainIndex;
        }
        catch
        {
            await _engineChain.RemoveChainLinkAsync(deploymentIdentifier).ConfigureAwait(false);
            await _deploymentPool.RemoveDeploymentAsync(deploymentIdentifier).ConfigureAwait(false);
            await _contextPool.UnregisterDeploymentAsync(deploymentIdentifier).ConfigureAwait(false);
            throw;
        }
    }

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _engineChain.StopAsync().ConfigureAwait(false);

        var startedDeployments = _deploymentPool.GetStartedDeployments();
        var stoppedDeployments = 0;
        foreach (var deployment in startedDeployments)
        {
            try
            {
                await _mediator.Send<StopEngine, bool>(new() { DeploymentIdentifier = deployment, }, cancellationToken).ConfigureAwait(false);
                stoppedDeployments++;
                _logger.StoppedDeployment(deployment);
            }
            catch (Exception ex)
            {
                _logger.StopDeploymentFailed(deployment, ex);
            }
        }
        _logger.StoppedDeployments(stoppedDeployments, startedDeployments.Count - stoppedDeployments, startedDeployments.Count);
    }

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
