using MQTTnet;

namespace ViciOne.ManagedEngine.Communication;

internal static class MqttApplicationMessageBuilderExtensions
{
    internal static MqttApplicationMessageBuilder WithRetainFlag(this MqttApplicationMessageBuilder builder, bool? retain)
    {
        if (retain.HasValue)
            return builder.WithRetainFlag(retain.Value);
        return builder.WithRetainFlag(true);
    }

    internal static MqttApplicationMessageBuilder WithMessageExpiryInterval(this MqttApplicationMessageBuilder builder, uint? expiryIntervalInSeconds)
    {
        if (expiryIntervalInSeconds.HasValue)
            builder.WithMessageExpiryInterval(expiryIntervalInSeconds.Value);
        return builder;
    }
}
