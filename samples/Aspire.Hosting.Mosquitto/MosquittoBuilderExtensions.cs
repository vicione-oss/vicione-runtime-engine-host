using System.IO;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Mosquitto;

public static class MosquittoBuilderExtensionss
{
    public static IResourceBuilder<MosquittoResource> AddMosquitto(this IDistributedApplicationBuilder builder,
        [ResourceName] string name, int port = MosquittoConfiguration.TcpPort)
    {
        MosquittoResource resource = new(name);

        var resourceBuilder = builder.AddResource(resource)
            .WithImage(MosquittoContainerImageTags.Image)
            .WithImageRegistry(MosquittoContainerImageTags.Registry)
            .WithImageTag(MosquittoContainerImageTags.Tag)
            .WithContainerFiles("/mosquitto/config",
            [
                new ContainerFile()
                {
                    Name = "mosquitto.conf",
                    Mode = UnixFileMode.GroupRead | UnixFileMode.UserRead,
                    Contents = $"""
                        per_listener_settings false
                        allow_anonymous true

                        listener {MosquittoConfiguration.TcpPort}
                        protocol mqtt
                        """,
                },
            ])
            .WithEndpoint(
                targetPort: MosquittoConfiguration.TcpPort,
                name: MosquittoResource.TcpEndpointName,
                port: port,
                scheme: "mqtt");

        return resourceBuilder;
    }
}
