using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.ManagedEngine.Communication;

public class CommunicationPayloadConverters_ConvertDeployEngine
{
    [Fact]
    public void Keeps_additional_information()
    {
        var engineStartParameter = """
        {
            "EngineStartParameter":
            {
                "Engines":
                [
                    {
                        "$id": 1,
                        "Name": "A"
                    },
                    {
                        "$id": 2,
                        "Name": "B"
                    }
                ],
                "EngineCommunications":
                [
                    {
                        "Source":
                        {
                            "$ref": 1
                        },
                        "Target":
                        {
                            "$ref": 2
                        },
                        "Com": "MQTT"
                    }
                ]
            }
        }
        """;
        DeployEnginePayload payload = new()
        {
            StartParameter = JsonSerializer.Deserialize<JsonElement>(engineStartParameter),
        };

        var request = CommunicationPayloadConverters.ToDeployEngine(payload);

        request.StartParameter.Should()
            .Contain("\"$id\": 1").And
            .Contain("\"$id\": 2").And
            .Contain("\"$ref\": 1").And
            .Contain("\"$ref\": 2");
    }
}
