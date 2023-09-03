using System;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class SqrtN : ScaleFishModelFunction
{
    public override double Compute(double n, double scale, double bias)
    {
        return scale * Math.Sqrt(n) + bias;
    }

    public override string Name { get; set; } = nameof(SqrtN);
    public override string OName { get; set; } = "O(sqrt(n))";
    public override string Quality { get; set; } = "Okay";
}