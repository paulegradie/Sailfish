namespace Sailfish.ComplexityEstimation.ComplexityFunctions;

public class Factorial : ComplexityFunction
{
    public override double Compute(int n)
    {
        if (n <= 1)
            return 1;

        double result = 1;
        for (var i = 2; i <= n; i++)
        {
            result *= i;
        }

        return result;
    }

    public override string Name { get; set; } = nameof(Factorial);
    public override string OName { get; set; } = "O(n!)";
    public override string Quality { get; set; } = "Worst!";
}