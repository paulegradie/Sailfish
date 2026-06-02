using System.IO;
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

        TrawlRunRecord? latest = null;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
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
            if (latest is null || record.TimestampUtc > latest.TimestampUtc) latest = record;
        }

        return latest;
    }
}
