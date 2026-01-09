using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ViciOne.ManagedEngine.ExternalCommunication;

[JsonDerivedType(typeof(Truck), nameof(Truck))]
[JsonDerivedType(typeof(Car), nameof(Car))]
[JsonDerivedType(typeof(Passenger), nameof(Passenger))]
internal interface IBase
{
    List<IBase> Children { get; }
}

internal sealed class Truck : IBase
{
    public List<IBase> Children { get; init; } = [];
    public bool HeavyDuty { get; set; }
}

internal sealed class Car : IBase
{
    public List<IBase> Children { get; init; } = [];
    public int Seats { get; set; }
}

internal sealed class Passenger : IBase
{
    public int Age { get; set; }
    List<IBase> IBase.Children { get; } = [];
}
