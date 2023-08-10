using System;
using Sailfish.Analysis.Scalefish.CurveFitting;

namespace Sailfish.Analysis.Scalefish.ComplexityFunctions;

public class Exponential : ComplexityFunction
{
    public Exponential(IFitnessCalculator fitnessCalculator) : base(fitnessCalculator)
    {
    }


    public override double Compute(double n, double scale, double bias)
    {
        return scale * Math.Pow(2, n) + bias;
    }

    public override string Name { get; set; } = nameof(Exponential);
    public override string OName { get; set; } = "O(2^n)";
    public override string Quality { get; set; } = "Very Bad";
}