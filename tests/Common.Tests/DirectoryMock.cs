using Testably.Abstractions.Testing;

namespace System.IO;

public sealed class DirectoryMock
{
    private readonly MockFileSystem _fileSystem;

    public DirectoryMock()
    {
        _fileSystem = new();
        Path = _fileSystem.Path.GetRandomFileName();
        _fileSystem.Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public MockFileSystem FileSystem => _fileSystem;

    public string CreateDirectory(params string[] pathParts)
    {
        var path = _fileSystem.Path.Combine(Path, _fileSystem.Path.Combine(pathParts));
        _fileSystem.Directory.CreateDirectory(path);
        return path;
    }

    public string CreateFile(params string[] pathParts)
    {
        var directoryPath = _fileSystem.Path.Combine(Path, _fileSystem.Path.Combine(pathParts[..^1]));
        _fileSystem.Directory.CreateDirectory(directoryPath);
        var filePath = _fileSystem.Path.Combine(Path, _fileSystem.Path.Combine(pathParts));
        _fileSystem.File.Create(filePath).Dispose();

        // Advance mock time to ensure unique timestamps
        ((MockTimeSystem)_fileSystem.TimeSystem).TimeProvider.AdvanceBy(TimeSpan.FromSeconds(1));

        return filePath;
    }
}
