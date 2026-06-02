using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Execution;

/// <summary>
///     Persists a Trawl run to the output directory: a machine-readable JSON record (consumed by regression
///     analysis) and the human-readable Markdown report, both under a <c>trawl/</c> subdirectory.
/// </summary>
internal sealed class TrawlResultWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string TrawlDirectory(string outputDirectory) => Path.Combine(outputDirectory, "trawl");

    public string PersistRecord(TrawlResult result, DateTime timestampUtc, string outputDirectory)
    {
        var path = Path.Combine(EnsureDirectory(outputDirectory), Stem(result, timestampUtc) + ".json");
        var record = new TrawlRunRecord { TimestampUtc = timestampUtc, Result = result };
        File.WriteAllText(path, JsonSerializer.Serialize(record, JsonOptions));
        return path;
    }

    public string WriteReport(string markdown, TrawlResult result, DateTime timestampUtc, string outputDirectory)
    {
        var path = Path.Combine(EnsureDirectory(outputDirectory), Stem(result, timestampUtc) + ".md");
        File.WriteAllText(path, markdown);
        return path;
    }

    private static string EnsureDirectory(string outputDirectory)
    {
        var dir = TrawlDirectory(outputDirectory);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string Stem(TrawlResult result, DateTime timestampUtc)
        => $"{Sanitize(result.DisplayName)}_{timestampUtc:yyyy-MM-ddTHH-mm-ss-fffZ}";

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(Array.IndexOf(invalid, c) >= 0 || c == ' ' ? '_' : c);
        return sb.Length == 0 ? "trawl" : sb.ToString();
    }
}
