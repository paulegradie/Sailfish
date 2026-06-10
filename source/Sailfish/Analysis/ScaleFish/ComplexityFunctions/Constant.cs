using System.Linq;
using Sailfish.Analysis.ScaleFish.CurveFitting;
using Sailfish.Exceptions;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

/// <summary>
/// O(1) family — the runtime does not depend on the variable at all. The model is <c>f(x) = bias</c>
/// with a single free parameter, which finally gives the AICc parameter penalty real work: for flat
/// data every two-parameter family can match Constant's residuals by fitting scale ≈ 0, but pays
/// 2·(k₂ − k₁) in AICc for the spare parameter, so the parsimonious model wins. A Constant
/// classification is also the strongest version of the "frozen scaling variable" signal —
/// <see cref="ConstantComplexityDetector"/> surfaces its usual hint whenever this family wins.
/// </summary>
public class Constant : ScaleFishModelFunction
{
    public override string Name { get; set; } = nameof(Constant);

    public override string OName { get; set; } = "O(1)";

    public override string Quality { get; set; } = "Best";

    public override string FunctionDef { get; set; } = "f(x) = {1}";

    /// <summary>Only the bias is fitted; scale is pinned to 0.</summary>
    public override int FreeParameterCount => 1;

    public override double Compute(double bias, double scale, double x)
    {
        return bias;
    }

    /// <summary>
    /// The generic linear-in-parameters fit cannot handle this family — its basis is the constant 1,
    /// which has zero variance. The maximum-likelihood constant under (weighted) Gaussian errors is
    /// simply the (weighted) mean of the observations.
    /// </summary>
    public override FittedCurve SeedFit(ComplexityMeasurement[] data, double[]? weights = null)
    {
        if (data is null || data.Length == 0)
            throw new SailfishException("At least one observation is required to fit a constant");
        if (weights is not null && weights.Length != data.Length)
            throw new SailfishException("weights length must match observations length");

        double sumW = 0, sumWy = 0;
        for (var i = 0; i < data.Length; i++)
        {
            var w = weights?[i] ?? 1.0;
            if (!double.IsFinite(w) || w < 0)
                throw new SailfishException("Weights must be non-negative and finite");
            if (!double.IsFinite(data[i].Y))
                throw new SailfishException("Non-finite observed value");
            sumW += w;
            sumWy += w * data[i].Y;
        }

        if (sumW <= 0) throw new SailfishException("Sum of weights must be positive");

        var mean = sumWy / sumW;
        if (!double.IsFinite(mean)) throw new SailfishException("Constant fit produced a non-finite mean");

        return new FittedCurve(scale: 0.0, bias: mean);
    }
}
