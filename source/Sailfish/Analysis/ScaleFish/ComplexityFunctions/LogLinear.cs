using System;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

/// <summary>
/// n·log₂(n) family. <strong>Deserialization-only:</strong> log₂(x) = ln(x)/ln(2), so this basis is a
/// constant multiple of <see cref="NLogN"/>'s and ordinary least squares produces identical fits for
/// both (the constant is absorbed into <c>scale</c>). The estimator therefore fits only
/// <see cref="NLogN"/>; this class remains registered so persisted models that classified as LogLinear
/// keep loading and predicting. To force it back into the candidate set, call
/// <c>ComplexityFunctionRegistry.Register&lt;LogLinear&gt;()</c> — at the cost of n·log n results never
/// being statistically distinguishable from their own clone.
/// </summary>
public class LogLinear : ScaleFishModelFunction
{
    public override string Name { get; set; } = nameof(LogLinear);

    public override string OName { get; set; } = "O(nlog_2(n))";

    public override string Quality { get; set; } = "Okay";

    public override string FunctionDef { get; set; } = "f(x) = {0}xLog_2(x) + {1}";

    public override double Compute(double bias, double scale, double x)
    {
        return scale * (x * Math.Log(x, 2)) + bias;
    }
}