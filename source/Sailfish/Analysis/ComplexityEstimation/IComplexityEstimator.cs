namespace Sailfish.Analysis.ComplexityEstimation;

public interface IComplexityEstimator
{
    ComplexityResult EstimateComplexity(ComplexityMeasurement[] measurements);
}