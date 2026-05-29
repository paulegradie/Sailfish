using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish.Trends;

/// <summary>
/// Compares two collections of <see cref="ComplexityHistoryEntry"/> snapshots and surfaces the
/// per-key transitions: stable, parameter drift, class changed, distinguishability flipped.
/// </summary>
public static class ComplexityHistoryDiffer
{
    /// <summary>
    /// Relative tolerance for declaring a fitted-parameter shift "material". Defaults to 25 %.
    /// </summary>
    public const double DefaultParameterDriftTolerance = 0.25;

    /// <summary>
    /// Returns one <see cref="ComplexityTransition"/> per key that appears in both snapshots, plus
    /// (optionally) entries for keys that exist only in <paramref name="current"/> as "new" entries.
    /// Keys present only in <paramref name="previous"/> are treated as removed and excluded.
    /// </summary>
    public static IReadOnlyList<ComplexityTransition> Diff(
        IEnumerable<ComplexityHistoryEntry> previous,
        IEnumerable<ComplexityHistoryEntry> current,
        double parameterDriftTolerance = DefaultParameterDriftTolerance)
    {
        if (previous is null) throw new ArgumentNullException(nameof(previous));
        if (current is null) throw new ArgumentNullException(nameof(current));

        // For each key, keep the most-recent prior entry (highest TimestampUtc) so we always compare
        // against the freshest known baseline.
        var priorByKey = previous
            .GroupBy(e => e.Key)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.TimestampUtc).First());

        var transitions = new List<ComplexityTransition>();
        foreach (var cur in current)
        {
            if (!priorByKey.TryGetValue(cur.Key, out var prior)) continue;
            transitions.Add(Compare(prior, cur, parameterDriftTolerance));
        }

        return transitions;
    }

    private static ComplexityTransition Compare(ComplexityHistoryEntry prev, ComplexityHistoryEntry cur, double drift)
    {
        if (!string.Equals(prev.BestFamilyName, cur.BestFamilyName, StringComparison.Ordinal))
        {
            return new ComplexityTransition(
                cur.Key,
                ComplexityTransitionKind.ClassChanged,
                prev, cur,
                $"{prev.BestFamilyOName} → {cur.BestFamilyOName} (was {prev.BestFamilyName}, now {cur.BestFamilyName})");
        }

        var relScaleChange = RelativeChange(prev.BestScale, cur.BestScale);
        if (double.IsFinite(relScaleChange) && relScaleChange > drift)
        {
            return new ComplexityTransition(
                cur.Key,
                ComplexityTransitionKind.ParameterDrift,
                prev, cur,
                $"scale drifted from {prev.BestScale:F4} to {cur.BestScale:F4} ({relScaleChange:P0})");
        }

        if (prev.IsDistinguishable != cur.IsDistinguishable)
        {
            return new ComplexityTransition(
                cur.Key,
                ComplexityTransitionKind.DistinguishabilityChanged,
                prev, cur,
                $"distinguishability flipped: was {(prev.IsDistinguishable ? "yes" : "no")}, now {(cur.IsDistinguishable ? "yes" : "no")}");
        }

        return new ComplexityTransition(cur.Key, ComplexityTransitionKind.Stable, prev, cur,
            $"stable on {cur.BestFamilyOName}");
    }

    private static double RelativeChange(double previous, double current)
    {
        if (!double.IsFinite(previous) || !double.IsFinite(current)) return double.NaN;
        if (previous == 0.0 && current == 0.0) return 0.0;
        var denom = Math.Max(Math.Abs(previous), 1e-12);
        return Math.Abs(current - previous) / denom;
    }
}
