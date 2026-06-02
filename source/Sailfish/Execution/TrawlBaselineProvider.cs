using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Execution;

/// <summary>
///     Loads the baseline for a Trawl scenario — the most recent prior <see cref="TrawlRunRecord" /> persisted
///     under <c>&lt;output&gt;/trawl/</c> for the same scenario. Because the current run is persisted only
///     after the comparison, "most recent on disk" is exactly the previous run.
/// </summary>
internal sealed class TrawlBaselineProvider
{
    public TrawlRunRecord? GetLatestBaseline(string displayName, string outputDirectory)
    {
        var dir = TrawlResultWriter.TrawlDirectory(outputDirectory);
        if (!Directory.Exists(dir)) return null;

        // Records are named "{Sanitize(displayName)}_{sortable-UTC-timestamp}.json", and the embedded
        // timestamp equals the record's TimestampUtc, so the lexically-greatest matching filename is the most
        // recent run. Filter to this scenario's candidates by name prefix (a cheap directory listing, no
        // reads), sort newest-first, then deserialize lazily and return the first exact DisplayName match —
        // touching at most a handful of files instead of deserializing every record in the directory.
        //
        // The prefix can still over-match a sibling whose sanitized name shares it (e.g. distinct names that
        // sanitize identically), which is why we verify DisplayName after deserializing.
        var prefix = TrawlResultWriter.Sanitize(displayName) + "_";
        var candidates = Directory.EnumerateFiles(dir, "*.json")
            .Where(path => Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal))
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal);

        foreach (var file in candidates)
        {
            TrawlRunRecord? record;
            try
            {
                record = JsonSerializer.Deserialize<TrawlRunRecord>(File.ReadAllText(file));
            }
            catch
            {
                continue; // skip unreadable/foreign files
            }

            if (record is null || record.Result.DisplayName != displayName) continue;
            return record;
        }

        return null;
    }
}
