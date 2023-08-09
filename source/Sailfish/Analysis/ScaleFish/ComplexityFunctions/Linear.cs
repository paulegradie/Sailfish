using Sailfish.Analysis.Scalefish.CurveFitting;

namespace Sailfish.Analysis.Scalefish.ComplexityFunctions;

public class Linear : ComplexityFunction
{
    public Linear(IFitnessCalculator fitnessCalculator) : base(fitnessCalculator)
    {
    }

    public override string Quality { get; set; } = "Good";
    public override string Name { get; set; } = nameof(Linear);
    public override string OName { get; set; } = "O(n)";

    public override double Compute(double n, double scale, double bias)
    {
        return scale * n + bias;
    }
}