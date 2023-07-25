using System;

namespace Sailfish.ComplexityEstimation.ComplexityFunctions;

public class Exponential : ComplexityFunction
{
    public override double Compute(int n)
    {
        return Math.Pow(2, n);
    }

    public override string Name { get; set; } = nameof(Exponential);
    public override string OName { get; set; } = "O(2^n)";
    public override string Quality { get; set; } = "Very Bad";
}