using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.EngineDeployment;

internal static class IMqttRpcClientExtensions
{
    internal static async Task<MqttRpcResponse> Request(this IMqttRpcClient client, MqttRpcRequest request, string id,
        CancellationToken cancellationToken)
    {
        var response = await client.Request(string.Concat(id, "/request"), request, string.Concat(id, "/response"), cancellationToken);

        if (string.Equals(response.Status, "ok", StringComparison.OrdinalIgnoreCase))
        {
            return response;
        }
        else
        {
            var reason = response.PlainText();
            throw new InvalidOperationException($"Could not execute {request.Method}. Reason: {reason}");
        }
    }
}
