using System;
using System.Linq;
using MathNet.Numerics;
using Sailfish.Exceptions;

namespace Sailfish.Analysis.ScaleFish.CurveFitting;

public interface IFitnessCalculator
{
    FittedCurve CalculateScaleAndBias(ComplexityMeasurement[] observations, Func<double, double, double, double> aFunc);

    FitnessResult CalculateError(double[] modeledValues, double[] observedValues);
}

public class FitnessCalculator : IFitnessCalculator
{
    public FittedCurve CalculateScaleAndBias(ComplexityMeasurement[] observations, Func<double, double, double, double> aFunc)
    {
        if (observations.Length == 0) throw new SailfishException("Arrays must not be empty");

        var xValues = observations.Select(x => x.X).ToArray();
        var yValues = observations.Select(x => x.Y).ToArray();

        var (bias, scale) = Fit.Curve(
            xValues,
            yValues,
            aFunc,
            yValues.Min(),
            1.0,
            maxIterations: 5000,
            tolerance: 1E-08);

        return new FittedCurve(scale, bias);
    }

    // 1. fit observations to each of the complexity functions with scale and bias to 'determine' the scale and bias
    // 2. Compute 'standard curve' using new scale and bias for the given function for all Xs
    // 3. Compute RMSE between standard fitted curve and emperical data
    // 4. Choose result with smalled RMSE
    public FitnessResult CalculateError(double[] modeledValues, double[] observedValues)
    {
        var n = observedValues.Length;
        if (modeledValues.Length != n) throw new ArgumentException("The length of the fittedValues array must match the length of the original data array.");

        var rSquared = RSquared(modeledValues, observedValues);
        var rmse = RMSE(modeledValues, observedValues);
        var meanAbsoluteError = Distance.MAE(modeledValues, observedValues);
        var sumOfAbsoluteDistance = Distance.SAD(modeledValues, observedValues);
        var sumOfSquaredDistance = Distance.SSD(modeledValues, observedValues);
        var meanSquaredError = Distance.MSE(modeledValues, observedValues);

        return new FitnessResult(
            rSquared,
            rmse,
            meanAbsoluteError,
            sumOfAbsoluteDistance,
            sumOfSquaredDistance,
            meanSquaredError);
    }

    private static double RSquared(double[] modeledValues, double[] observedValues)
    {
        return GoodnessOfFit.RSquared(modeledValues, observedValues);
    }

    private static double RMSE(double[] modeledValues, double[] observedValues)
    {
        return Math.Sqrt(observedValues.Zip(modeledValues, (y, yFit) => Math.Pow(y - yFit, 2)).Sum() / observedValues.Length);
    }
}