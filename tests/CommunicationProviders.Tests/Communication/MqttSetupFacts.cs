using System;
using AwesomeAssertions;
using MQTTnet.Formatter;
using Xunit;

namespace ViciOne.ManagedEngine.Communication;

public class MqttSetup_ToCommunicationInfo
{
    [Fact]
    public void Configures_tcp_server()
    {
        MqttCommunication communication = new()
        {
            Host = "localhost",
        };

        var communicationInfo = communication.ToCommunicationInfo(Guid.Empty);

        communicationInfo.Should().Be(new MQTTnet.Extensions.CommunicationInfo()
        {
            TcpHost = communication.Host,
            ProtocolVersion = MqttProtocolVersion.V500,
            ClientId = $"eh-com-{Guid.Empty}"
        });
    }

    [Fact]
    public void Configures_web_socket_server()
    {
        MqttCommunication communication = new()
        {
            Uri = new Uri("localhost:8000/exchange"),
        };

        var communicationInfo = communication.ToCommunicationInfo(Guid.Empty);

        communicationInfo.Should().Be(new MQTTnet.Extensions.CommunicationInfo()
        {
            WebSocketUri = communication.Uri,
            ProtocolVersion = MqttProtocolVersion.V500,
            ClientId = $"eh-com-{Guid.Empty}"
        });
    }

    [Fact]
    public void Does_not_throw_for_unconfigured_endpoint()
    {
        MqttCommunication communication = new();

        var communicationInfo = communication.ToCommunicationInfo(Guid.Empty);

        communicationInfo.Should().Be(new MQTTnet.Extensions.CommunicationInfo()
        {
            ProtocolVersion = MqttProtocolVersion.V500,
            ClientId = $"eh-com-{Guid.Empty}"
        });
    }

    [Theory]
    [InlineData("", "abc")]
    [InlineData("user", "abc")]
    [InlineData("user", "")]
    public void Configures_credentials(string username, string password)
    {
        MqttCommunication communication = new()
        {
            Uri = new Uri("localhost:8000/exchange"),
            Username = username,
            Password = password,
        };

        var communicationInfo = communication.ToCommunicationInfo(Guid.Empty);

        communicationInfo.Should().Be(new MQTTnet.Extensions.CommunicationInfo()
        {
            WebSocketUri = communication.Uri,
            ProtocolVersion = MqttProtocolVersion.V500,
            Username = communication.Username,
            Password = communication.Password,
            ClientId = $"eh-com-{Guid.Empty}"
        });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(null, null)]
    public void Configures_clean_session(bool? cleanSession, bool? configuredCleanSession)
    {
        MqttCommunication communication = new()
        {
            Uri = new Uri("localhost:8000/exchange"),
            CleanSession = cleanSession,
        };

        var communicationInfo = communication.ToCommunicationInfo(Guid.Empty);

        communicationInfo.CleanSession.Should().Be(configuredCleanSession);
    }

    [Theory]
    [InlineData(10u, 10u)]
    [InlineData(0u, 0u)]
    [InlineData(null, null)]
    public void Configures_session_expiry_interval(uint? interval, uint? configuredInterval)
    {
        MqttCommunication communication = new()
        {
            Uri = new Uri("localhost:8000/exchange"),
            SessionExpiryInterval = interval,
        };

        var communicationInfo = communication.ToCommunicationInfo();

        communicationInfo.SessionExpiryInterval.Should().Be(configuredInterval);
    }

    [Theory]
    [InlineData(100_000)]
    [InlineData(null)]
    public void Configures_max_pending_messages(int? max)
    {
        MqttCommunication communication = new()
        {
            MaxPendingMessages = max,
        };

        var communicationInfo = communication.ToCommunicationInfo();

        communicationInfo.MaxPendingMessages.Should().Be(max);
    }
}
