namespace Sailfish.Contracts.Public.Models;

/// <summary>The outcome of comparing a Trawl run against its baseline.</summary>
public enum TrawlRegressionOutcome
{
    /// <summary>Significantly faster than baseline.</summary>
    Improved,

    /// <summary>Significantly slower than baseline.</summary>
    Regressed,

    /// <summary>No statistically significant change.</summary>
    NotSignificant,

    /// <summary>Could not be determined (no baseline, no samples, or the test failed).</summary>
    Inconclusive
}

/// <summary>
///     The result of a Trawl regression comparison: a current run's latency distribution tested against the
///     baseline (most recent prior) run via SailDiff's statistical machinery.
/// </summary>
public sealed record TrawlRegressionVerdict
{
    /// <summary>The categorical outcome of the comparison (improved / regressed / not significant / inconclusive).</summary>
    public TrawlRegressionOutcome Outcome { get; init; }

    /// <summary>Percentage change in mean latency vs baseline; positive means slower (a regression).</summary>
    public double PercentChange { get; init; }

    /// <summary>The statistical test's p-value.</summary>
    public double PValue { get; init; }

    /// <summary>Human-readable verdict line.</summary>
    public string Message { get; init; } = string.Empty;
}
