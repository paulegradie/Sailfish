using System;
using System.Collections.Generic;

namespace Sailfish.Diagnostics.Environment;

public enum HealthStatus
{
    Pass,
    Warn,
    Fail,
    Unknown
}

public class HealthCheckEntry
{
    public string Name { get; }
    public HealthStatus Status { get; }
    public string Details { get; }
    public string? Recommendation { get; }

    public HealthCheckEntry(string name, HealthStatus status, string details, string? recommendation = null)
    {
        Name = name;
        Status = status;
        Details = details;
        Recommendation = recommendation;
    }
}

public class EnvironmentHealthReport
{
    public IReadOnlyList<HealthCheckEntry> Entries { get; }
    public int Score { get; }

    public EnvironmentHealthReport(IReadOnlyList<HealthCheckEntry> entries)
    {
        Entries = entries;
        Score = ComputeScore(entries);
    }

    private static int ComputeScore(IReadOnlyList<HealthCheckEntry> entries)
    {
        if (entries.Count == 0) return 0;
        var total = 0;
        foreach (var e in entries)
        {
            total += e.Status switch
            {
                HealthStatus.Pass => 10,
                HealthStatus.Warn => 6,
                HealthStatus.Fail => 0,
                HealthStatus.Unknown => 4,
                _ => 0
            };
        }
        var max = entries.Count * 10;
        var pct = (int)Math.Round(100.0 * total / max);
        return Math.Clamp(pct, 0, 100);
    }

    public string SummaryLabel => Score switch
    {
        >= 85 => "Excellent",
        >= 70 => "Good",
        >= 50 => "Fair",
        _ => "Poor"
    };
}

