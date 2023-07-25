namespace Sailfish.ComplexityEstimation;

public interface IComplexityFunction
{
    double Compute(int n);
    string Name { get; set; }
    string OName { get; set; }
    string Quality { get; set; }

    double ComputeError(ComplexityMeasurement[] referenceData);
}