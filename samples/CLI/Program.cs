using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ManagedEngine;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

CommandLineParameters parameters = new();
configuration.Bind(parameters);
var factories = DeployParameterFactory.FindAll();
var settings = UI.ConfigureApp(parameters, factories);
await App.RunAsync(settings, CancellationToken.None);
