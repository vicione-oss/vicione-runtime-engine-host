namespace ViciOne.ManagedEngine.Communication;

/// <summary>
/// Communication configuration for file system-based operations.
/// </summary>
[Communication("8B58C1F9-D120-42E9-8F63-B7C62FE1C415")]
public class FileSystemCommunication : ICommunication
{
    /// <summary>
    /// Gets or sets the directory path to setup operations.
    /// </summary>
    public string Directory { get; set; } = string.Empty;
}
