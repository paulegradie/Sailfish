using System;

namespace Sailfish.ComplexityEstimation.ComplexityFunctions;

public class LogLogN : ComplexityFunction
{
    public override double Compute(int n)
    {
        return Math.Log(Math.Log(n, 2), 2);
    }

    public override string Name { get; set; } = nameof(LogLogN);
    public override string OName { get; set; } = "O(log(log(n)))";
    public override string Quality { get; set; } = "Okay";
}