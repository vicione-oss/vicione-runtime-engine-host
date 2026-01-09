using System.IO.Abstractions;
using Testably.Abstractions;
using Testably.Abstractions.Testing;

namespace System.IO;

public sealed class TemporaryDirectory : IDisposable
{
    private bool _disposedValue;
    private readonly IFileSystem _fileSystem;

    public TemporaryDirectory(bool useMock = true)
    {
        if (useMock)
            _fileSystem = new MockFileSystem();
        else
            _fileSystem = new RealFileSystem();

        Path = _fileSystem.Path.GetRandomFileName();
        _fileSystem.Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public IFileSystem FileSystem => _fileSystem;

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

        if (_fileSystem is MockFileSystem mock)
            ((MockTimeSystem)mock.TimeSystem).TimeProvider.AdvanceBy(TimeSpan.FromSeconds(1));

        return filePath;
    }

    public void Dispose()
    {
        if (!_disposedValue)
        {
            if (_fileSystem.Directory.Exists(Path))
            {
                try
                {
                    _fileSystem.Directory.Delete(Path, true);
                }
                catch
                {
                }
            }
            _disposedValue = true;
        }
    }
}
