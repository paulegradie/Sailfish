namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class Linear : ScaleFishModelFunction
{
    public override double Compute(double n, double scale, double bias)
    {
        return scale * n + bias;
    }

    public override string Quality { get; set; } = "Good";

    public override string Name { get; set; } = nameof(Linear);

    public override string OName { get; set; } = "O(n)";

    public override string FunctionDef { get; set; } = "f(x) = {0}x + {1}";
}