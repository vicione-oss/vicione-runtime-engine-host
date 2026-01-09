using System.Collections.Generic;

namespace ViciOne.ManagedEngine.Communication;

[Communication("96B50074-1DF9-402C-90CF-AC9894C8E243")]
public class JournalCommunication : ICommunication
{
    public IReadOnlyDictionary<string, object?>? MetaDataFields { get; set; }
    public string? OutputTemplate { get; set; }
}
