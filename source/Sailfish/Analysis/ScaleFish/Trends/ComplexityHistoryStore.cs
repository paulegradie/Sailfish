using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Sailfish.Analysis.ScaleFish.Trends;

/// <summary>
/// File-system persistence for <see cref="ComplexityHistoryEntry"/> snapshots. Writes one JSON file per
/// run in the tracking directory, indexed by a sortable filename so the diff process can pick up the
/// most-recent prior file deterministically.
/// </summary>
public static class ComplexityHistoryStore
{
    /// <summary>
    /// Subdirectory inside the tracking directory where history files live. Keeps them separate from
    /// model files and execution-summary tracking files.
    /// </summary>
    public const string HistoryDirectoryName = "ComplexityHistory";

    private const string FilePrefix = "ComplexityHistory_";

    /// <summary>
    /// Writes <paramref name="entries"/> to a new history file named with the timestamp + commit SHA so
    /// the most-recent file sorts last alphabetically. Returns the path written.
    /// </summary>
    public static string Write(string trackingDirectory, IReadOnlyList<ComplexityHistoryEntry> entries, DateTime utcNow, string commitSha)
    {
        if (entries is null) throw new ArgumentNullException(nameof(entries));
        if (string.IsNullOrWhiteSpace(trackingDirectory))
            throw new ArgumentException("trackingDirectory must be non-empty", nameof(trackingDirectory));

        var dir = Path.Combine(trackingDirectory, HistoryDirectoryName);
        Directory.CreateDirectory(dir);

        var fileName = $"{FilePrefix}{utcNow:yyyyMMdd-HHmmss}_{Sanitize(commitSha)}.json";
        var path = Path.Combine(dir, fileName);

        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        return path;
    }

    /// <summary>
    /// Loads the most-recently-written history file (by filename ordering) from the tracking directory,
    /// optionally excluding a specific filename. Returns an empty list if none exist.
    /// </summary>
    public static IReadOnlyList<ComplexityHistoryEntry> LoadMostRecentPrior(string trackingDirectory, string? excludeFilename = null)
    {
        var dir = Path.Combine(trackingDirectory, HistoryDirectoryName);
        if (!Directory.Exists(dir)) return Array.Empty<ComplexityHistoryEntry>();

        var candidates = Directory.GetFiles(dir, $"{FilePrefix}*.json")
            .Where(p => excludeFilename is null || !string.Equals(Path.GetFileName(p), excludeFilename, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0) return Array.Empty<ComplexityHistoryEntry>();

        try
        {
            var json = File.ReadAllText(candidates[0]);
            return JsonSerializer.Deserialize<List<ComplexityHistoryEntry>>(json) ?? new List<ComplexityHistoryEntry>();
        }
        catch
        {
            // Malformed or unreadable prior file shouldn't break the run — just skip the diff.
            return Array.Empty<ComplexityHistoryEntry>();
        }
    }

    /// <summary>
    /// Resolves a commit SHA from common CI env vars, falling back to <c>git rev-parse HEAD</c> and
    /// finally to "unknown" when nothing is available.
    /// </summary>
    public static string ResolveCommitSha()
    {
        string?[] envVars = { "GITHUB_SHA", "CI_COMMIT_SHA", "BUILD_SOURCEVERSION", "CIRCLE_SHA1", "GIT_COMMIT" };
        foreach (var v in envVars)
        {
            var value = Environment.GetEnvironmentVariable(v!);
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse HEAD",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            if (process.WaitForExit(2000))
            {
                var sha = process.StandardOutput.ReadToEnd().Trim();
                if (!string.IsNullOrWhiteSpace(sha) && sha.Length >= 7) return sha;
            }
            else
            {
                // Timeout — git is still running. `using` would dispose without terminating;
                // kill it explicitly so we don't leak an orphaned child process.
                try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            }
        }
        catch
        {
            // git not available — fall through
        }

        return "unknown";
    }

    private static string Sanitize(string sha)
    {
        if (string.IsNullOrWhiteSpace(sha)) return "unknown";
        var safe = new char[Math.Min(sha.Length, 12)];
        for (var i = 0; i < safe.Length; i++)
        {
            var c = sha[i];
            safe[i] = char.IsLetterOrDigit(c) ? c : '-';
        }
        return new string(safe);
    }
}
