using System;

namespace Sailfish.ComplexityEstimation.ComplexityFunctions;

public class NLogN : ComplexityFunction
{
    public override double Compute(int n)
    {
        return n * Math.Log(n);
    }

    public override string Name { get; set; } = nameof(NLogN);
    public override string OName { get; set; } = "O(nLog(n))";
    public override string Quality { get; set; } = "Good";
}