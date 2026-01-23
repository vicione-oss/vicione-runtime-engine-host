using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ManagedEngine.Communication;

[SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "Intended static key holder")]
internal sealed class MqttConnectionKeys
{
    internal const string CommandBus = "commandbus";
    internal const string Monitoring = "monitoring";
}
