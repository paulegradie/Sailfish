using System;

namespace Sailfish.ComplexityEstimation.ComplexityFunctions;

public class LogLinear : ComplexityFunction
{
    public override double Compute(int n)
    {
        return n * Math.Log(n, 2);
    }

    public override string Name { get; set; } = "LogLinear";
    public override string OName { get; set; } = "O(nlog2(n)";
    public override string Quality { get; set; } = "Okay";
}