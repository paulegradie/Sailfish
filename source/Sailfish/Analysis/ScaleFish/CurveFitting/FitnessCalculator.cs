using System;
using System.Linq;
using MathNet.Numerics;
using Sailfish.Exceptions;

namespace Sailfish.Analysis.ScaleFish.CurveFitting;

public interface IFitnessCalculator
{
    /// <summary>
    /// Legacy entry: fits scale/bias for the model y = aFunc(bias, scale, x) using MathNet's non-linear least squares.
    /// Preserved for backward compatibility; new code paths prefer the linear-in-parameters fits performed on
    /// <see cref="ScaleFishModelFunction"/>.
    /// </summary>
    FittedCurve CalculateScaleAndBias(ComplexityMeasurement[] observations, Func<double, double, double, double> aFunc);

    /// <summary>
    /// Linear-in-parameters OLS: y_i = scale * basis(x_i) + bias.
    /// When all <paramref name="weights"/> are equal (or null) this is ordinary least squares; otherwise it
    /// is weighted least squares (weights = 1 / variance_i).
    /// </summary>
    FittedCurve FitLinearInParameters(ComplexityMeasurement[] observations, Func<double, double> basis, double[]? weights = null);

    /// <summary>
    /// Computes goodness-of-fit metrics. Note: historically this was called with the modeled/observed roles
    /// swapped; we preserve that calling convention so existing fit reports remain comparable.
    /// </summary>
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

    public FittedCurve FitLinearInParameters(ComplexityMeasurement[] observations, Func<double, double> basis, double[]? weights = null)
    {
        if (observations is null) throw new SailfishException("observations must not be null");
        if (observations.Length < 2) throw new SailfishException("At least 2 observations are required for a linear-in-parameters fit");
        if (weights is not null && weights.Length != observations.Length)
            throw new SailfishException("weights length must match observations length");

        var n = observations.Length;
        var basisValues = new double[n];

        // Pass 1: validate inputs and accumulate weighted means.
        double sumW = 0, sumWf = 0, sumWy = 0;
        for (var i = 0; i < n; i++)
        {
            var f = basis(observations[i].X);
            var y = observations[i].Y;
            if (!double.IsFinite(f) || !double.IsFinite(y))
                throw new SailfishException("Non-finite basis or observed value");

            var w = weights?[i] ?? 1.0;
            if (!double.IsFinite(w) || w < 0)
                throw new SailfishException("Weights must be non-negative and finite");

            basisValues[i] = f;
            sumW += w;
            sumWf += w * f;
            sumWy += w * y;
        }

        if (sumW <= 0) throw new SailfishException("Sum of weights must be positive");
        var fMean = sumWf / sumW;
        var yMean = sumWy / sumW;

        // Pass 2: weighted centred second moments (numerically stable; avoids catastrophic cancellation
        // between sumWff and fMean^2 when the basis spans many orders of magnitude — e.g. Factorial).
        double sumWdf2 = 0, sumWdfdy = 0;
        for (var i = 0; i < n; i++)
        {
            var df = basisValues[i] - fMean;
            var dy = observations[i].Y - yMean;
            var w = weights?[i] ?? 1.0;
            sumWdf2 += w * df * df;
            sumWdfdy += w * df * dy;
        }

        if (sumWdf2 < 1e-30)
            throw new SailfishException("Basis values have effectively zero variance — cannot fit");

        var scale = sumWdfdy / sumWdf2;
        var bias = yMean - scale * fMean;

        if (!double.IsFinite(scale) || !double.IsFinite(bias))
            throw new SailfishException("Fit produced non-finite parameters");

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
        var rmse = Rmse(modeledValues, observedValues);
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

    private static double Rmse(double[] modeledValues, double[] observedValues)
    {
        return Math.Sqrt(observedValues.Zip(modeledValues, (y, yFit) => Math.Pow(y - yFit, 2)).Sum() / observedValues.Length);
    }
}
