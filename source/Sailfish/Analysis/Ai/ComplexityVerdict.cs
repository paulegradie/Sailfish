using System.Collections.Generic;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     Grounded ScaleFish result for one (method, variable) pair: the best-fit Big-O class, how well it fit, the
///     runner-up, whether the data can actually tell them apart, and concrete projections to larger N. As with
///     every figure in the context, the agent <em>explains</em> these — it never computes them.
/// </summary>
public sealed record ComplexityVerdict(
    string TestMethodName,
    string PropertyName,
    string BestFitComplexity,
    double GoodnessOfFit,
    string NextBestComplexity,
    double NextBestGoodnessOfFit,
    bool IsDistinguishable,
    int? SuggestedNextN,
    IReadOnlyList<ComplexityProjection> Projections);

/// <summary>A projection of the fitted curve to a given N — "at N items, expect roughly PredictedValue ms".</summary>
public sealed record ComplexityProjection(int N, double PredictedValue);
