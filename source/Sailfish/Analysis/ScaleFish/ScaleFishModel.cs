namespace Sailfish.Analysis.ScaleFish;

public class ScaleFishModel(
    ScaleFishModelFunction scaleFishModelFunction,
    double goodnessOfFit,
    ScaleFishModelFunction nextClosestScaleFishModelFunction,
    double nextClosestGoodnessOfFit)
{
    public ScaleFishModelFunction ScaleFishModelFunction { get; init; } = scaleFishModelFunction;
    public double GoodnessOfFit { get; init; } = goodnessOfFit;
    public ScaleFishModelFunction NextClosestScaleFishModelFunction { get; init; } = nextClosestScaleFishModelFunction;
    public double NextClosestGoodnessOfFit { get; init; } = nextClosestGoodnessOfFit;
}