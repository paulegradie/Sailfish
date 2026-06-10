using System;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

/// <summary>
/// O(log n) family — binary search, balanced-tree and index lookups, skip lists. Deliberately the
/// <em>only</em> logarithmic family: log bases differ by a constant factor that the fitted
/// <c>scale</c> absorbs, so a log₂/log₁₀ sibling would be collinear with this one and re-create the
/// NLogN/LogLinear clone problem (two identical fits tying for first place and destroying
/// distinguishability).
/// </summary>
public class Logarithmic : ScaleFishModelFunction
{
    public override string Name { get; set; } = nameof(Logarithmic);

    public override string OName { get; set; } = "O(log(n))";

    public override string Quality { get; set; } = "Excellent";

    public override string FunctionDef { get; set; } = "f(x) = {0}Log_e(x) + {1}";

    public override double Compute(double bias, double scale, double x)
    {
        // x ≤ 0 yields NaN/-∞, which AnalyzeFitness treats as "family not applicable to this data".
        return scale * Math.Log(x) + bias;
    }
}
