using System;

namespace ViciOne.ManagedEngine.ExternalCommunication;

internal record SubscriberInfo(int ContextHash, Action<string> HandleValueAbstractly, Action<ExternalValue> HandleValueDirectly);
