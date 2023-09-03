using System;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class Exponential : ScaleFishModelFunction
{
    public override double Compute(double n, double scale, double bias)
    {
        return scale * Math.Pow(2, n) + bias;
    }

    public override string Name { get; set; } = nameof(Exponential);
    public override string OName { get; set; } = "O(2^n)";
    public override string Quality { get; set; } = "Very Bad";
}