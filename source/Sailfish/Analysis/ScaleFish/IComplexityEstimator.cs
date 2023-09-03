namespace Sailfish.Analysis.ScaleFish;

public interface IComplexityEstimator
{
    ComplexityResult EstimateComplexity(ComplexityMeasurement[] measurements);
}