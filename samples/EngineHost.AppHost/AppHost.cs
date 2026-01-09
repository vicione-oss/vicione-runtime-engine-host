using Aspire.Hosting.Mosquitto;

var builder = DistributedApplication.CreateBuilder(args);

var mqtt = builder.AddMosquitto("mqtt", 1883);

builder.AddProject<Projects.EngineHost>("enginehost")
    .WithReference(mqtt)
    .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WaitFor(mqtt);

builder.Build().Run();
