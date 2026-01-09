namespace ViciOne.ManagedEngine.Communication;

[Communication("8B58C1F9-D120-42E9-8F63-B7C62FE1C415")]
public class FileSystemCommunication : ICommunication
{
    public string Directory { get; set; } = string.Empty;
}
