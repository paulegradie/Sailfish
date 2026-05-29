using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;

namespace Sailfish.Analysis.ScaleFish;

/// <summary>
/// Pre-run helper that recommends an optimal X probe set for a ScaleFish run, given a target N range and
/// (optionally) a prior <see cref="ScaleFishModel"/>. Geometric spacing is used by default — equally
/// spaced in log-x gives the most discrimination per data point.
///
/// <para>
/// This is a planning utility — the user copies the recommended values into a
/// <see cref="SailfishVariableAttribute"/> or <see cref="SailfishRangeVariableAttribute"/> for the next
/// run. We deliberately don't drive the execution loop directly so the recommender stays a pure function
/// and integrates with any test runner without pipeline changes.
/// </para>
/// </summary>
public static class AdaptiveXRecommender
{
    /// <summary>
    /// Recommend a geometric probe set in [<paramref name="minN"/>, <paramref name="maxN"/>] with
    /// <paramref name="points"/> points. Equivalent to <see cref="SailfishRangeVariableAttribute.SpacedRange"/>
    /// in Geometric mode but exposed as a planning helper.
    /// </summary>
    public static IReadOnlyList<int> RecommendInitialProbe(int minN, int maxN, int points = 6)
    {
        if (points < 2) throw new ArgumentException("points must be ≥ 2", nameof(points));
        if (minN < 1) throw new ArgumentException("minN must be ≥ 1", nameof(minN));
        if (maxN <= minN) throw new ArgumentException("maxN must be > minN", nameof(maxN));
        if (points > maxN - minN + 1)
            throw new ArgumentException("points exceeds available integer values in [minN, maxN]");

        return SailfishRangeVariableAttribute.SpacedRange(minN, maxN, points, RangeSpacing.Geometric).ToArray();
    }

    /// <summary>
    /// Given a prior ScaleFish run and a target upper bound, recommend the next probe set. Strategy:
    /// extend the current max X geometrically up to <paramref name="targetMaxN"/> and add enough new
    /// points so the curves diverge enough to flip the classification's <c>IsDistinguishable</c> flag.
    /// </summary>
    /// <param name="previousProbeXs">The X values that produced <paramref name="previous"/>.</param>
    /// <param name="previous">The model fitted on <paramref name="previousProbeXs"/>.</param>
    /// <param name="targetMaxN">Desired upper bound for the next probe set.</param>
    /// <param name="extraPoints">How many *new* X values to add beyond the previous max. Defaults to 3.</param>
    public static IReadOnlyList<int> RecommendRefinement(
        IReadOnlyList<int> previousProbeXs,
        ScaleFishModel previous,
        int targetMaxN,
        int extraPoints = 3)
    {
        if (previousProbeXs is null || previousProbeXs.Count == 0)
            throw new ArgumentException("previousProbeXs must contain at least one X", nameof(previousProbeXs));
        if (previous is null) throw new ArgumentNullException(nameof(previous));
        if (extraPoints < 1) throw new ArgumentException("extraPoints must be ≥ 1", nameof(extraPoints));

        var existing = previousProbeXs.OrderBy(x => x).Distinct().ToList();
        var currentMax = existing[^1];
        if (targetMaxN <= currentMax) return existing;

        // If the previous run flagged a suggested next N, use it as a floor for where the extension
        // begins so the geometric extension starts at the divergence point.
        var floor = previous.SuggestedNextN.HasValue
            ? Math.Max(currentMax + 1, previous.SuggestedNextN.Value)
            : currentMax + 1;

        if (floor > targetMaxN)
            // The suggested next N already exceeds the target — extend from current max via geometric
            // doubling, ignoring the suggestion. Caller can raise targetMaxN if desired.
            floor = currentMax + 1;

        // SpacedRange requires count ≤ (end - start + 1) distinct integers. Cap extraPoints so a
        // narrow extension range (e.g. floor = currentMax + 1 with default extraPoints = 3) degrades
        // to whatever is achievable rather than throwing.
        var availableIntegers = targetMaxN - floor + 1;
        if (availableIntegers < 2)
        {
            // Even a single new point won't satisfy SpacedRange's count >= 2 contract — fall back to
            // appending the single available integer at the end of the existing set.
            if (availableIntegers == 1) existing.Add(floor);
            return existing.Distinct().OrderBy(x => x).ToList();
        }

        var cappedExtraPoints = Math.Min(extraPoints, availableIntegers);
        var extras = SailfishRangeVariableAttribute
            .SpacedRange(floor, targetMaxN, cappedExtraPoints, RangeSpacing.Geometric)
            .ToList();

        // Merge with existing, dedup, sort.
        return existing.Concat(extras).Distinct().OrderBy(x => x).ToList();
    }
}
