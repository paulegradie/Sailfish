using System;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class LogLinear : ScaleFishModelFunction
{
    public override string Name { get; set; } = nameof(LogLinear);

    public override string OName { get; set; } = "O(nlog_2(n))";

    public override string Quality { get; set; } = "Okay";

    public override string FunctionDef { get; set; } = "f(x) = {0}xLog_2(x) + {1}";

    public override double Compute(double bias, double scale, double x)
    {
        return scale * (x * Math.Log(x, 2)) + bias;
    }
}