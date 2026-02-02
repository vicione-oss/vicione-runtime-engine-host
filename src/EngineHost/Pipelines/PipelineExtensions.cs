using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ManagedEngine.PackageResolver;
using ViciOne.ManagedEngine.Pipelines.Behaviors;
using ViciOne.ManagedEngine.Pipelines.Requests;

namespace ViciOne.ManagedEngine.Pipelines;

internal static class PipelineExtensions
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddMediator()
            => services
                .AddMediatorCore()
                .AddMediatorPipeline();

        private IServiceCollection AddMediatorCore()
            => services.AddSingleton<IMediator, Mediator>();

        private IServiceCollection AddMediatorPipeline()
            => services
                .AddTransient<IRequestHandler<DeployEngine, string>, DeployEngineHandler>()
                .AddTransient<IRequestHandler<StartEngine, bool>, StartEngineHandler>()
                .AddTransient<IRequestHandler<StopEngine, bool>, StopEngineHandler>()
                .AddTransient<IRequestHandler<TearDownEngine, bool>, TearDownEngineHandler>()
                .AddTransient<IRequestHandler<GetState, EngineState>, GetStateHandler>()
                .AddTransient<IRequestHandler<GetPackages, IReadOnlyCollection<PackageReference>>, GetPackagesHandler>()
                .AddTransient<IRequestHandler<GetDeploymentStates, IReadOnlyDictionary<string, EngineState>>, GetDeploymentStatesHandler>()
                .AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    }
}
