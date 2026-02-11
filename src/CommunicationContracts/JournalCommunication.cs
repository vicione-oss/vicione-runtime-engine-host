using System.Collections.Generic;

namespace ViciOne.ManagedEngine.Communication;

/// <summary>
/// Communication configuration for systemd journal logging.
/// </summary>
[Communication("96B50074-1DF9-402C-90CF-AC9894C8E243")]
public class JournalCommunication : ICommunication
{
    /// <summary>
    /// Gets or sets additional metadata fields to include in journal entries.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? MetaDataFields { get; set; }

    /// <summary>
    /// Gets or sets the output template for formatting log messages.
    /// </summary>
    public string? OutputTemplate { get; set; }
}
