using System;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class Exponential : ScaleFishModelFunction
{
    public override double Compute(double bias, double scale, double x)
    {
        return scale * Math.Pow(2, x) + bias;
    }

    public override string Name { get; set; } = nameof(Exponential);
    public override string OName { get; set; } = "O(2^n)";
    public override string Quality { get; set; } = "Very Bad";
    public override string FunctionDef { get; set; } = "f(x) = {0}*(2^x) + {1}";

}