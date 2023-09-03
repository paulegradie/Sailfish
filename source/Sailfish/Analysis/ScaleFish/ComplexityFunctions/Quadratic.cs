using System;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class Quadratic : ComplexityFunction
{
    public override double Compute(double n, double scale, double bias)
    {
        return scale * Math.Pow(n, 2) + bias;
    }

    public override string Name { get; set; } = nameof(Quadratic);
    public override string OName { get; set; } = "O(n^2)";
    public override string Quality { get; set; } = "Bad";
}