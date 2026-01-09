using System;
using System.IO;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.Runtime;

internal static class FileSystem
{
    internal static async Task<string> TryReadAsync(string path, int retryCount = 3)
    {
        while (true)
        {
            try
            {
                await using var stream = new FileStream(path, new FileStreamOptions()
                {
                    Mode = FileMode.Open,
                    Access = FileAccess.Read,
                    Share = FileShare.ReadWrite,
                    Options = FileOptions.Asynchronous,
                });
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                if (--retryCount == 0)
                    throw;
            }
        }
    }
}
