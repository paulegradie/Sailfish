namespace Sailfish.ComplexityEstimation.ComplexityFunctions;

public class Linear : ComplexityFunction
{
    public override string Quality { get; set; } = "Good";
    public override string Name { get; set; } = nameof(Linear);
    public override string OName { get; set; } = "O(n)";

    public override double Compute(int n)
    {
        return n;
    }
}