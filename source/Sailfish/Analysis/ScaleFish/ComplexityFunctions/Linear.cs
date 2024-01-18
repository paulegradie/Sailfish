namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class Linear : ScaleFishModelFunction
{
    public override string Quality { get; set; } = "Good";

    public override string Name { get; set; } = nameof(Linear);

    public override string OName { get; set; } = "O(n)";

    public override string FunctionDef { get; set; } = "f(x) = {0}x + {1}";

    public override double Compute(double bias, double scale, double x)
    {
        return scale * x + bias;
    }
}