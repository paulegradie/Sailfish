namespace Sailfish.Analysis.ScaleFish;

public class ScaleFishModel
{
    public ScaleFishModel(ScaleFishModelFunction scaleFishModelFunction,
        double goodnessOfFit,
        ScaleFishModelFunction nextClosestScaleFishModelFunction,
        double nextClosestGoodnessOfFit)
    {
        ScaleFishModelFunction = scaleFishModelFunction;
        GoodnessOfFit = goodnessOfFit;
        NextClosestScaleFishModelFunction = nextClosestScaleFishModelFunction;
        NextClosestGoodnessOfFit = nextClosestGoodnessOfFit;
    }

    public ScaleFishModelFunction ScaleFishModelFunction { get; init; }
    public double GoodnessOfFit { get; init; }
    public ScaleFishModelFunction NextClosestScaleFishModelFunction { get; init; }
    public double NextClosestGoodnessOfFit { get; init; }
}