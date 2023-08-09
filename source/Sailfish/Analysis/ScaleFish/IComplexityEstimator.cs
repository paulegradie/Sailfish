namespace Sailfish.Analysis.Scalefish;

public interface IComplexityEstimator
{
    ComplexityResult EstimateComplexity(ComplexityMeasurement[] measurements);
}