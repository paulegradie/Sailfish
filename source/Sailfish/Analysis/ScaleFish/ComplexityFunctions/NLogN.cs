using System;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class NLogN : ScaleFishModelFunction
{
    public override string Name { get; set; } = nameof(NLogN);
    public override string OName { get; set; } = "O(nLog(n))";
    public override string Quality { get; set; } = "Good";
    public override string FunctionDef { get; set; } = "f(x) = {0}xLog_e(x) + {1}";

    public override double Compute(double bias, double scale, double x)
    {
        return scale * (x * Math.Log(x)) + bias;
    }
}