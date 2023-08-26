namespace Sailfish.Analysis.Scalefish;

public class ComplexityResult
{
    public ComplexityResult(ComplexityFunction complexityFunction, double goodnessOfFit, ComplexityFunction nextClosestComplexityFunction, double nextClosestGoodnessOfFit)
    {
        this.ComplexityFunction = complexityFunction;
        this.GoodnessOfFit = goodnessOfFit;
        this.NextClosestComplexityFunction = nextClosestComplexityFunction;
        this.NextClosestGoodnessOfFit = nextClosestGoodnessOfFit;
    }

    public ComplexityFunction ComplexityFunction { get; init; }
    public double GoodnessOfFit { get; init; }
    public ComplexityFunction NextClosestComplexityFunction { get; init; }
    public double NextClosestGoodnessOfFit { get; init; }

    public void Deconstruct(out ComplexityFunction complexityFunction, out double goodnessOfFit, out ComplexityFunction nextClosestComplexityFunction, out double nextClosestGoodnessOfFit)
    {
        complexityFunction = this.ComplexityFunction;
        goodnessOfFit = this.GoodnessOfFit;
        nextClosestComplexityFunction = this.NextClosestComplexityFunction;
        nextClosestGoodnessOfFit = this.NextClosestGoodnessOfFit;
    }
}