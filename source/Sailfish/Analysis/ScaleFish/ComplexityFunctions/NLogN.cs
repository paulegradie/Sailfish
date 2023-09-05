using System;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class NLogN : ScaleFishModelFunction
{
    public override double Compute(double n, double scale, double bias)
    {
        return scale * (n * Math.Log(n)) + bias;
    }

    public override string Name { get; set; } = nameof(NLogN);
    public override string OName { get; set; } = "O(nLog(n))";
    public override string Quality { get; set; } = "Good";
    public override string FunctionDef { get; set; } = "f(x) = {0}xLog_e(x) + {1}";

}