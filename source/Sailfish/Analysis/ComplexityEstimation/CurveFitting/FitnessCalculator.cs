using System;
using System.Linq;
using MathNet.Numerics;
using Sailfish.Exceptions;

namespace Sailfish.Analysis.ComplexityEstimation.CurveFitting;

public class FitnessCalculator : IFitnessCalculator
{
    public FittedCurve CalculateScaleAndBias(ComplexityMeasurement[] observations, Func<double, double, double, double> aFunc)
    {
        if (observations.Length == 0) throw new SailfishException("Arrays must not be empty");

        var xValues = observations.Select(x => x.X).ToArray();
        var yValues = observations.Select(x => x.Y).ToArray();

        var (scale, bias) = Fit.Curve(
            xValues,
            yValues,
            aFunc,
            1,
            1,
            maxIterations: 10_000,
            tolerance: 1E-04D);

        return new FittedCurve(scale, bias);
    }

    // 1. fit observations to each of the complexity functions with scale and bias to 'determine' the scale and bias
    // 2. Compute 'standard curve' using new scale and bias for the given function for all Xs
    // 3. Compute RMSE between standard fitted curve and emperical data
    // 4. Choose result with smalled RMSE
    public FitnessResult CalculateError(double[] modeledValues, double[] observedValues)
    {
        var n = observedValues.Length;
        if (modeledValues.Length != n)
        {
            throw new ArgumentException("The length of the fittedValues array must match the length of the original data array.");
        }

        // Calculate R-squared to evaluate goodness of fit
        var rSquared = GoodnessOfFit.RSquared(modeledValues, observedValues);

        // Calculate root mean square error (RMSE) to evaluate goodness of fit
        var rmse = Math.Sqrt(observedValues.Zip(modeledValues, (y, yFit) => Math.Pow(y - yFit, 2)).Sum() / n);

        return new FitnessResult(rSquared, rmse);
    }
}