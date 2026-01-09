using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using MQTTnet.Extensions;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Xunit;

namespace ViciOne.ManagedEngine;

public class HostConfig_Apply
{
    private static Dictionary<string, string?> GetMinimalSettings()
        => new()
        {
            ["Id"] = "",
            ["CommandBus:Type"] = "Tcp",
            ["CommandBus:Host"] = "",
            ["DeploymentsDirectory"] = "",
            ["PackagesDirectory"] = "",
            ["PreloadTypeConverterAssemblyInContext"] = "",
        };
    private const string ConnectionName = "mqtt";

    [Fact]
    public void Reads_default_configuration()
    {
        var appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        HostConfig actual = new();

        HostConfig.Apply(actual, appsettings, ConnectionName);

        actual.Should().BeEquivalentTo(new HostConfig()
        {
            CommandBus = new()
            {
                ProtocolVersion = MqttProtocolVersion.V500,
                TcpHost = "localhost",
                TcpPort = 1883,
                Password = string.Empty,
                Username = string.Empty,
                WillTopic = "1/state",
                WillMessage = "interruption",
                WillContentType = MediaTypeNames.Text.Plain,
                WillQualityOfService = MqttQualityOfServiceLevel.AtMostOnce,
                ClientId = "eh-1-commandbus"
            },
            DeploymentsDirectory = "deployments",
            Id = "1",
            Monitoring = new()
            {
                ProtocolVersion = MqttProtocolVersion.V500,
                WebSocketUri = new("ws://localhost:9001/ws"),
                Password = string.Empty,
                Username = string.Empty,
                ClientId = "eh-1-monitoring"
            },
            PackagesDirectory = "packages",
            PreloadTypeConverterAssemblyInContext = true,
        }, options => options.IncludingInternalProperties().PreferringRuntimeMemberTypes());
    }

    [Theory]
    [InlineData(nameof(HostConfig.DeploymentsDirectory))]
    [InlineData(nameof(HostConfig.Id))]
    [InlineData(nameof(HostConfig.PackagesDirectory))]
    public void Throws_on_missing_properties(string property)
    {
        var settings = GetMinimalSettings();
        settings.Remove(property);
        var appsettings = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        HostConfig actual = new();

        FluentActions.Invoking(() => HostConfig.Apply(actual, appsettings, ConnectionName)).Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"*failed*get*value*{property}*");
    }

    [Fact]
    public void Throws_on_missing_CommandBus_section()
    {
        var settings = GetMinimalSettings();
        foreach (var setting in settings.Keys.Where(x => x.StartsWith(nameof(HostConfig.CommandBus), StringComparison.OrdinalIgnoreCase)))
            settings.Remove(setting);
        var appsettings = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        HostConfig actual = new();

        FluentActions.Invoking(() => HostConfig.Apply(actual, appsettings, ConnectionName)).Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*section*CommandBus*not*found*");
    }

    [Fact]
    public void Uses_CommandBus_for_Monitoring_if_Monitoring_section_is_missing()
    {
        var settings = GetMinimalSettings();
        var appsettings = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        HostConfig actual = new();

        HostConfig.Apply(actual, appsettings, ConnectionName);

        actual.Should().BeEquivalentTo(new HostConfig()
        {
            CommandBus = new()
            {
                ProtocolVersion = MqttProtocolVersion.V500,
                TcpHost = "",
                TcpPort = 0,
                WillTopic = "/state",
                WillMessage = "interruption",
                WillContentType = MediaTypeNames.Text.Plain,
                WillQualityOfService = MqttQualityOfServiceLevel.AtMostOnce,
                ClientId = "eh--commandbus"
            },
            Monitoring = new()
            {
                ProtocolVersion = MqttProtocolVersion.V500,
                TcpHost = "",
                TcpPort = 0,
                WillTopic = "/state",
                WillMessage = "interruption",
                WillContentType = MediaTypeNames.Text.Plain,
                WillQualityOfService = MqttQualityOfServiceLevel.AtMostOnce,
                ClientId = "eh--commandbus"
            },
            PreloadTypeConverterAssemblyInContext = true,
        }, options => options.IncludingInternalProperties().PreferringRuntimeMemberTypes());
    }

    [Fact]
    public void Uses_Monitoring_section_if_specified()
    {
        var settings = GetMinimalSettings();
        settings["Monitoring:Type"] = "WebSocket";
        settings["Monitoring:Url"] = "ws://localhost";
        var appsettings = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        HostConfig actual = new();

        HostConfig.Apply(actual, appsettings, ConnectionName);

        actual.Should().BeEquivalentTo(new HostConfig()
        {
            CommandBus = new()
            {
                ProtocolVersion = MqttProtocolVersion.V500,
                TcpHost = "",
                TcpPort = 0,
                WillTopic = "/state",
                WillMessage = "interruption",
                WillContentType = MediaTypeNames.Text.Plain,
                WillQualityOfService = MqttQualityOfServiceLevel.AtMostOnce,
                ClientId = "eh--commandbus"
            },
            Monitoring = new()
            {
                ProtocolVersion = MqttProtocolVersion.V500,
                WebSocketUri = new("ws://localhost"),
                ClientId = "eh--monitoring"
            },
            PreloadTypeConverterAssemblyInContext = true,
        }, options => options.IncludingInternalProperties().PreferringRuntimeMemberTypes());
    }

    [Theory]
    [InlineData(nameof(HostConfig.CommandBus))]
    [InlineData(nameof(HostConfig.Monitoring))]
    public void Throws_on_missing_mqtt_type(string section)
    {
        var settings = GetMinimalSettings();
        settings.Add(nameof(HostConfig.Monitoring), "");
        settings.Remove($"{section}:Type");
        var appsettings = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        HostConfig actual = new();

        FluentActions.Invoking(() => HostConfig.Apply(actual, appsettings, ConnectionName)).Should()
            .Throw<NotSupportedException>()
            .WithMessage($"*{section}:Type*''*not*supported*");
    }

    [Theory]
    [InlineData(nameof(HostConfig.CommandBus))]
    [InlineData(nameof(HostConfig.Monitoring))]
    public void Throws_on_unsupported_mqtt_type(string section)
    {
        var settings = GetMinimalSettings();
        settings[$"{section}:Type"] = "unknown";
        var appsettings = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        HostConfig actual = new();

        FluentActions.Invoking(() => HostConfig.Apply(actual, appsettings, ConnectionName)).Should()
            .Throw<NotSupportedException>()
            .WithMessage($"*{section}:Type*'unknown'*not*supported*");
    }

    [Theory]
    [InlineData(nameof(HostConfig.CommandBus))]
    [InlineData(nameof(HostConfig.Monitoring))]
    public void Throws_on_missing_mqtt_tcp_host(string section)
    {
        var settings = GetMinimalSettings();
        settings[$"{section}:Type"] = "Tcp";
        settings.Remove($"{section}:Host");
        settings[$"{section}:Port"] = "0";
        var appsettings = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        HostConfig actual = new();

        FluentActions.Invoking(() => HostConfig.Apply(actual, appsettings, ConnectionName)).Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"*failed*get*value*{section}:Host*");
    }

    [Theory]
    [InlineData(nameof(HostConfig.CommandBus))]
    [InlineData(nameof(HostConfig.Monitoring))]
    public void Uses_zero_as_mqtt_default_port_if_missing(string section)
    {
        var settings = GetMinimalSettings();
        settings[$"{section}:Type"] = "Tcp";
        settings[$"{section}:Host"] = "localhost";
        settings.Remove($"{section}:Port");
        var appsettings = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        HostConfig actual = new();

        HostConfig.Apply(actual, appsettings, ConnectionName);

        var port = actual.GetType().GetProperty(section, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(actual);
        port.Should().BeOfType<CommunicationInfo>().Which.TcpPort.Should().Be(0);
    }

    [Theory]
    [InlineData(nameof(HostConfig.CommandBus))]
    [InlineData(nameof(HostConfig.Monitoring))]
    public void Throws_on_missing_mqtt_websocket_url(string section)
    {
        var settings = GetMinimalSettings();
        settings[$"{section}:Type"] = "WebSocket";
        settings.Remove($"{section}:Url");
        var appsettings = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        HostConfig actual = new();

        FluentActions.Invoking(() => HostConfig.Apply(actual, appsettings, ConnectionName)).Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"*failed*get*value*{section}:Url*");
    }
}
