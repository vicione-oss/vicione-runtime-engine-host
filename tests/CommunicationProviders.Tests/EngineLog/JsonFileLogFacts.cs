using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ViciOne.ManagedEngine.EngineLog;

public class JsonFileLog_AppendLogEntryAsync
{
    [Fact]
    public async Task Writes_entry_to_missing_file_Async()
    {
        using var directory = new TemporaryDirectory();
        var engineId = "2303-01";
        LogEntry entry = new() { Timestamp = new DateTime(1, 1, 5), Type = LogLevel.Information, Text = "Start", };
        var log = new JsonFileLog(directory.Path, engineId, directory.FileSystem);

        await log.AppendLogEntryAsync(entry, CancellationToken.None);

        var file = directory.FileSystem.Path.Combine(directory.Path, string.Concat(engineId, ".json"));
        var lines = await directory.FileSystem.File.ReadAllLinesAsync(file);
        var actual = lines.Select(e => JsonSerializer.Deserialize<LogEntry>(e, JsonFileLogContext.Options)!).ToList();
        var expected = new[]
        {
            new LogEntry { Timestamp = new DateTime(1, 1, 5), Type = LogLevel.Information, Text = "Start", },
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Appends_entry_to_existing_file_Async()
    {
        using var directory = new TemporaryDirectory();
        LogEntry existingEntry = new() { Timestamp = new DateTime(1, 1, 5), Type = LogLevel.Information, Text = "Start", };
        var engineId = "2303-01";
        var file = directory.CreateFile(string.Concat(engineId, ".json"));
        directory.FileSystem.File.WriteAllLines(file, [JsonSerializer.Serialize(existingEntry, JsonFileLogContext.Options),]);
        var newEntry = new LogEntry { Timestamp = new DateTime(1, 1, 11), Type = LogLevel.Debug, Text = "Stop", };
        var log = new JsonFileLog(directory.Path, engineId, directory.FileSystem);

        await log.AppendLogEntryAsync(newEntry, CancellationToken.None);

        var lines = await directory.FileSystem.File.ReadAllLinesAsync(file);
        var actual = lines.Select(e => JsonSerializer.Deserialize<LogEntry>(e, JsonFileLogContext.Options)!).ToList();
        var expected = new[]
        {
            new LogEntry { Timestamp = new DateTime(1, 1, 5), Type = LogLevel.Information, Text = "Start", },
            new LogEntry { Timestamp = new DateTime(1, 1, 11), Type = LogLevel.Debug, Text = "Stop", },
        };
        actual.Should().BeEquivalentTo(expected);
    }
}

public class JsonFileLog_ReadLogEntriesAsync
{
    [Fact]
    public async Task Reads_entries_Async()
    {
        using var directory = new TemporaryDirectory();
        var engineId = "2303-01";
        var entries = new[]
        {
            new LogEntry { Timestamp = new DateTime(1, 1, 5), Type = LogLevel.Information, Text = "Start", },
            new LogEntry { Timestamp = new DateTime(1, 1, 11), Type = LogLevel.Debug, Text = "Stop", },
        };
        var file = directory.CreateFile(string.Concat(engineId, ".json"));
        directory.FileSystem.File.WriteAllLines(file, [.. entries.Select(e => JsonSerializer.Serialize(e, JsonFileLogContext.Options))]);
        var log = new JsonFileLog(directory.Path, engineId, directory.FileSystem);

        var actual = await log.ReadLogEntriesAsync();

        var expected = new[]
        {
            new LogEntry { Timestamp = new DateTime(1, 1, 5), Type = LogLevel.Information, Text = "Start", },
            new LogEntry { Timestamp = new DateTime(1, 1, 11), Type = LogLevel.Debug, Text = "Stop", },
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Returns_empty_list_if_file_does_not_exists_Async()
    {
        using var directory = new TemporaryDirectory();
        var log = new JsonFileLog(directory.Path, "2303-01", directory.FileSystem);

        var entries = await log.ReadLogEntriesAsync();

        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_empty_list_if_file_is_empty_Async()
    {
        using var directory = new TemporaryDirectory();
        var engineId = "2303-01";
        var log = new JsonFileLog(directory.Path, engineId, directory.FileSystem);
        var file = directory.CreateFile(engineId);

        var entries = await log.ReadLogEntriesAsync();

        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Throws_exception_on_read_error_Async()
    {
        var engineId = "2303";
        using var directory = new TemporaryDirectory();
        var file = directory.CreateFile(string.Concat(engineId, ".json"));
        var log = new JsonFileLog(directory.Path, engineId, directory.FileSystem);
        await using var stream = directory.FileSystem.FileStream.New(file, FileMode.Open, FileAccess.Read, FileShare.None);

        var act = log.ReadLogEntriesAsync;

        await act.Should().ThrowAsync<IOException>().WithMessage("*cannot*access*2303*");
    }
}

internal static class JsonFileLogContext
{
    internal static JsonSerializerOptions Options { get; } = new()
    {
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };
}
