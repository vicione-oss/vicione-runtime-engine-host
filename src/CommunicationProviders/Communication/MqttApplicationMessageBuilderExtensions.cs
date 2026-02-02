using MQTTnet;

namespace ViciOne.ManagedEngine.Communication;

internal static class MqttApplicationMessageBuilderExtensions
{
    extension(MqttApplicationMessageBuilder builder)
    {
        internal MqttApplicationMessageBuilder WithRetainFlag(bool? retain)
        {
            if (retain.HasValue)
                return builder.WithRetainFlag(retain.Value);
            return builder.WithRetainFlag(true);
        }

        internal MqttApplicationMessageBuilder WithMessageExpiryInterval(uint? expiryIntervalInSeconds)
        {
            if (expiryIntervalInSeconds.HasValue)
                builder.WithMessageExpiryInterval(expiryIntervalInSeconds.Value);
            return builder;
        }
    }
}
