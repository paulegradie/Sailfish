using System;

namespace Sailfish.Analysis.ComplexityEstimation.CurveFitting;

public interface IFitnessCalculator
{
    FittedCurve CalculateScaleAndBias(ComplexityMeasurement[] observations, Func<double, double, double, double> aFunc);
    FitnessResult CalculateError(double[] modeledValues, double[] observedValues);
}