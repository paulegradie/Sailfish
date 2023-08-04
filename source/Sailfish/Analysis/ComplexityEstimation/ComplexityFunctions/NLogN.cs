using System;
using Sailfish.Analysis.ComplexityEstimation.CurveFitting;

namespace Sailfish.Analysis.ComplexityEstimation.ComplexityFunctions;

public class NLogN : ComplexityFunction
{
    public NLogN(IFitnessCalculator fitnessCalculator) : base(fitnessCalculator)
    {
    }

    public override double Compute(double n, double scale, double bias)
    {
        return scale * (n * Math.Log(n)) + bias;
    }

    public override string Name { get; set; } = nameof(NLogN);
    public override string OName { get; set; } = "O(nLog(n))";
    public override string Quality { get; set; } = "Good";
}