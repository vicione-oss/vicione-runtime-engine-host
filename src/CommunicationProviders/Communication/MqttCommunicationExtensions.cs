namespace ViciOne.ManagedEngine.Communication;

internal static class MqttCommunicationExtensions
{
    internal static string DescribeEndpoint(this MqttCommunication communication)
    {
        if (!string.IsNullOrWhiteSpace(communication.Host))
        {
            if (communication.Port.HasValue)
                return $"{communication.Host}:{communication.Port}";
            else
                return communication.Host;
        }
        else if (communication.Uri is not null)
        {
            return communication.Uri.ToString();
        }
        else
        {
            return string.Empty;
        }
    }
}
