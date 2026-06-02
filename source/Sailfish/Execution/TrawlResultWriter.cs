using System;
using System.IO;
using System.Linq;
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

    /// <summary>
    ///     Prunes a scenario's persisted runs (the <c>.json</c> record and its matching <c>.md</c> report) down
    ///     to the <paramref name="maxRetained" /> most recent, by filename timestamp. A value &lt;= 0 (the
    ///     default) keeps every run — no pruning. Best-effort: a file that can't be deleted is skipped, never
    ///     thrown. Records are matched by the same sanitized-name prefix used to write them, so two display
    ///     names that sanitize to an identical stem share one retention pool.
    /// </summary>
    public void PruneOldRecords(TrawlResult result, string outputDirectory, int maxRetained)
    {
        if (maxRetained <= 0) return;

        var dir = TrawlDirectory(outputDirectory);
        if (!Directory.Exists(dir)) return;

        var prefix = Sanitize(result.DisplayName) + "_";
        var stale = Directory.EnumerateFiles(dir, "*.json")
            .Where(path => Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal))
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal) // newest first (timestamp stem sorts lexically)
            .Skip(maxRetained)
            .ToList();

        foreach (var json in stale)
        {
            TryDelete(json);
            TryDelete(Path.ChangeExtension(json, ".md"));
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup — a locked/removed artifact must never fail the run.
        }
    }

    private static string EnsureDirectory(string outputDirectory)
    {
        var dir = TrawlDirectory(outputDirectory);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string Stem(TrawlResult result, DateTime timestampUtc)
        => $"{Sanitize(result.DisplayName)}_{timestampUtc:yyyy-MM-ddTHH-mm-ss-fffZ}";

    /// <summary>
    ///     Maps a display name to the filename stem prefix used for its persisted records: invalid filename
    ///     characters and spaces become <c>_</c>. Shared with <see cref="TrawlBaselineProvider" /> so baseline
    ///     lookup can enumerate only a scenario's own candidate files.
    /// </summary>
    internal static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(Array.IndexOf(invalid, c) >= 0 || c == ' ' ? '_' : c);
        return sb.Length == 0 ? "trawl" : sb.ToString();
    }
}
