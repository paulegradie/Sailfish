using System;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class Cubic : ScaleFishModelFunction
{
    public override double Compute(double bias, double scale, double x)
    {
        return (scale * Math.Pow(x, 3)) + bias;
    }

    public override string Name { get; set; } = nameof(Cubic);
    public override string OName { get; set; } = "O(n^3)";
    public override string Quality { get; set; } = "Very Bad";
    public override string FunctionDef { get; set; } = "f(x) = {0}x^3 + {1}";
}