using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ViciOne.ManagedEngine.EngineLog;

[SuppressMessage("Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht", Justification = "Factory übernimmt Dispose von LoggerProvider")]
[SuppressMessage("Performance", "CA1848:LoggerMessage-Delegaten verwenden", Justification = "Testet den Logger an sich")]
public class JsonFileLoggerProvider_
{
    [Fact]
    public void Acceptance()
    {
        DirectoryMock directory = new();
        using var factory = new LoggerFactory();
        factory.AddProvider(new JsonFileLoggerProvider(new()
        {
            Directory = directory.Path,
            EngineUniqueIdentifier = "engine-A",
        }, directory.FileSystem));
        var logger = factory.CreateLogger("Test");

        logger.LogWarning("Engine Zyklus übersprungen");
        logger.LogDebug("Beginne nächsten Zyklus");

        factory.Dispose();
        var lines = directory.FileSystem.File.ReadLines(directory.FileSystem.Path.Combine(directory.Path, "engine-A.json")).ToArray();
        lines.Should().HaveCount(2);
        lines[0].Should().ContainEquivalentOf("warning");
        lines[1].Should().ContainEquivalentOf("debug");
    }

    public class ProcessLogQueueMethod
    {
        [Fact]
        public void Empties_old_log_file()
        {
            DirectoryMock directory = new();
            var logFile = directory.FileSystem.Path.Combine(directory.Path, "engine-A.json");
            directory.FileSystem.File.WriteAllText(logFile, "alt");

            using var factory = new LoggerFactory();
            factory.AddProvider(new JsonFileLoggerProvider(new()
            {
                Directory = directory.Path,
                EngineUniqueIdentifier = "engine-A",
            }, directory.FileSystem));
            factory.Dispose();

            directory.FileSystem.File.ReadAllText(logFile).Should().BeEmpty();
        }
    }
}
