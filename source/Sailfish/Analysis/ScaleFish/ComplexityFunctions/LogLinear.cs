using System;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class LogLinear : ScaleFishModelFunction
{
    public override double Compute(double n, double scale, double bias)
    {
        return scale * (n * Math.Log(n, 2)) + bias;
    }

    public override string Name { get; set; } = nameof(LogLinear);

    public override string OName { get; set; } = "O(nlog_2(n))";

    public override string Quality { get; set; } = "Okay";

    public override string FunctionDef { get; set; } = "f(x) = {0}xLog_2(x) + {1}";
}