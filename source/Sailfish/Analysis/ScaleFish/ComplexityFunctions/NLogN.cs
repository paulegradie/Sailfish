using System;

namespace Sailfish.Analysis.Scalefish.ComplexityFunctions;

public class NLogN : ComplexityFunction
{
    public override double Compute(double n, double scale, double bias)
    {
        return scale * (n * Math.Log(n)) + bias;
    }

    public override string Name { get; set; } = nameof(NLogN);
    public override string OName { get; set; } = "O(nLog(n))";
    public override string Quality { get; set; } = "Good";
}