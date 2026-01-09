using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.EngineDeployment;

internal sealed class EngineHostCommunication
{
    private readonly IMqttRpcClient _mqttClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter(),
            new TypeJsonConverter(),
        },
        ReferenceHandler = ReferenceHandler.Preserve,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    internal EngineHostCommunication(IMqttRpcClient mqttClient) => _mqttClient = mqttClient;

    internal async Task<string> DeployAsync(string engineHostUniqueIdentifier, DeployParameter deployParameter, CancellationToken cancellationToken)
    {
        var startParameterAsJObject = JsonSerializer.SerializeToNode(deployParameter.StartParameter, _jsonOptions);
        DeployPayload deployPayload = new(startParameterAsJObject!, deployParameter.PackageReferences,
            deployParameter.EngineChainIndex, deployParameter.CycleTime);
        var mqttRequest = MqttRpcRequest.GZipJson<object>("deploy", deployPayload);
        var response = await _mqttClient.Request(mqttRequest, engineHostUniqueIdentifier, cancellationToken);
        return response.GZipJson<string>();
    }

    internal async Task<EngineState> GetStateAsync(string engineHostUniqueIdentifier, string deploymentIdentifier, CancellationToken cancellationToken)
    {
        var request = MqttRpcRequest.Json("getstate", deploymentIdentifier);
        var response = await _mqttClient.Request(request, engineHostUniqueIdentifier, cancellationToken);
        var state = response.Json<EngineState>();
        return state;
    }

    internal async Task<bool> StartAsync(string engineHostUniqueIdentifier, string deploymentIdentifier, CancellationToken cancellationToken)
    {
        var request = MqttRpcRequest.Json("start", deploymentIdentifier);
        var response = await _mqttClient.Request(request, engineHostUniqueIdentifier, cancellationToken);
        return response.Json<bool>();
    }

    internal async Task<bool> StopAsync(string engineHostUniqueIdentifier, string deploymentIdentifier, CancellationToken cancellationToken)
    {
        var request = MqttRpcRequest.Json("stop", deploymentIdentifier);
        var response = await _mqttClient.Request(request, engineHostUniqueIdentifier, cancellationToken);
        return response.Json<bool>();
    }

    internal async Task TearDownAsync(string engineHostUniqueIdentifier, string deploymentIdentifier, CancellationToken cancellationToken)
    {
        var request = MqttRpcRequest.Json("teardown", deploymentIdentifier);
        await _mqttClient.Request(request, engineHostUniqueIdentifier, cancellationToken);
    }

    private sealed record class DeployPayload(object StartParameter, IReadOnlyCollection<PackageReference> PackageReferences, int EngineChainIndex, uint CycleTime);
}
