using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Mosquitto;

public sealed class MosquittoResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string TcpEndpointName = "tcp";

    private EndpointReference? _tcpEndPoint;
    public EndpointReference TcpEndPoint => _tcpEndPoint ??= new(this, TcpEndpointName);

    public ReferenceExpression ConnectionStringExpression
        => ReferenceExpression.Create($"{TcpEndPoint.Property(EndpointProperty.Url)}");
}
