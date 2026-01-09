using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.EngineDeployment;

internal interface IMqttRpcClient
{
    internal Task<MqttRpcResponse> Request(string requestTopic, MqttRpcRequest request, string responseTopic, CancellationToken cancellationToken);
}
