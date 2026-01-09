using System;

namespace ViciOne.ManagedEngine.Runtime;

internal sealed record class TimerTick<T>(TimeSpan Duration, T Output);
