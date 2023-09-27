using System;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class SqrtN : ScaleFishModelFunction
{
    public override double Compute(double bias, double scale, double x)
    {
        return scale * Math.Sqrt(x) + bias;
    }

    public override string Name { get; set; } = nameof(SqrtN);
    public override string OName { get; set; } = "O(sqrt(n))";
    public override string Quality { get; set; } = "Okay";
    public override string FunctionDef { get; set; } = "f(x) = {0}sqrt(x) + {1}";
}