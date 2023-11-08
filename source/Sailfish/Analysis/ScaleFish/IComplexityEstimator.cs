namespace Sailfish.Analysis.ScaleFish;

public interface IComplexityEstimator
{
    ScalefishModel? EstimateComplexity(ComplexityMeasurement[] measurements);
}