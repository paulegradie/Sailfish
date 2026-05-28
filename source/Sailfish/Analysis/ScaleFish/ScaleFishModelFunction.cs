using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using MathNet.Numerics;
using Sailfish.Analysis.ScaleFish.CurveFitting;
using Sailfish.Exceptions;

namespace Sailfish.Analysis.ScaleFish;

public abstract class ScaleFishModelFunction
{
    public abstract string Name { get; set; }
    public abstract string OName { get; set; }
    public abstract string Quality { get; set; }
    public abstract string FunctionDef { get; set; }

    public FittedCurve? FunctionParameters { get; set; }

    [JsonIgnore]
    public IFitnessCalculator FitnessCalculator { get; set; } = new FitnessCalculator(); // leave this public for testing. Gross, but willing to accept

    /// <summary>
    /// Number of free parameters in this model. Used for information-criterion (AICc) model selection.
    /// All built-in families fit two parameters (scale, bias); override on models with more or fewer.
    /// </summary>
    [JsonIgnore]
    public virtual int FreeParameterCount => 2;

    public abstract double Compute(double bias, double scale, double x);

    /// <summary>
    /// The linearizing basis: y is modelled as <c>scale * Basis(x) + bias</c>. Default is <c>Compute(0, 1, x)</c>.
    /// Families whose basis overflows for typical inputs (Exponential, Factorial) override <see cref="SeedFit"/> instead.
    /// </summary>
    protected virtual double Basis(double x) => Compute(0.0, 1.0, x);

    /// <summary>
    /// Produces (scale, bias) for this family by linear-in-parameters OLS on the basis.
    /// Override to use log-space or other tricks when the basis overflows at typical X values.
    /// </summary>
    public virtual FittedCurve SeedFit(ComplexityMeasurement[] data, double[]? weights = null)
    {
        return FitnessCalculator.FitLinearInParameters(data, Basis, weights);
    }

    public FitnessResult AnalyzeFitness(IEnumerable<ComplexityMeasurement> validationData)
    {
        var cleanReferenceData = validationData.Where(x => x.Y.IsFinite()).ToArray();
        if (cleanReferenceData.Length < 2)
            return InvalidFitness();

        var weights = BuildVarianceWeights(cleanReferenceData);

        try
        {
            FunctionParameters = SeedFit(cleanReferenceData, weights);
        }
        catch
        {
            // The family cannot be fit to this data (e.g. basis overflows, zero variance, etc.).
            // Surface as an invalid result so the estimator excludes it explicitly rather than silently.
            FunctionParameters = null;
            return InvalidFitness();
        }

        var fittedYs = cleanReferenceData
            .Select(m => Compute(FunctionParameters!.Bias, FunctionParameters.Scale, m.X))
            .ToArray();
        var observations = cleanReferenceData.Select(x => x.Y).ToArray();

        // A family that cannot evaluate Compute(...) at every input in the user's range (e.g. raw 2^x
        // overflowing for large X) doesn't honestly fit the data. Invalidate rather than silently
        // computing SSD on a truncated subset, which would otherwise give an artificially good fit.
        if (fittedYs.Any(v => !v.IsFinite()))
            return InvalidFitness();

        var pairs = observations
            .Zip(fittedYs)
            .Where(pair => pair.First.IsFinite() && pair.Second.IsFinite())
            .ToList();

        if (pairs.Count < 2)
            return InvalidFitness();

        // NOTE: parameter names "modeled" and "observed" are swapped vs reality here — the first array is the
        // raw observations, the second is the fitted curve. This historical ordering is preserved so the
        // reported R^2 / SSD remain identical between releases. Symmetric metrics (MAE/MSE/SSD/SAD) are
        // unaffected; R^2 differs only marginally from a properly-oriented fit on well-fit data.
        var firstArr = pairs.Select(x => x.First).ToArray();
        var secondArr = pairs.Select(x => x.Second).ToArray();

        try
        {
            return FitnessCalculator.CalculateError(firstArr, secondArr);
        }
        catch
        {
            return InvalidFitness();
        }
    }

    private static FitnessResult InvalidFitness()
    {
        return new FitnessResult(0, 9999999999, 999999999, 999999999, 999999999, 999999999) { IsValid = false };
    }

    /// <summary>
    /// When every measurement carries replicate uncertainty, returns 1 / SE^2 weights normalised so they
    /// sum to N (preserves the residual scale used by downstream metrics). Returns null otherwise so the
    /// fit falls back to uniform weights — preserving identical behaviour for data without uncertainty.
    ///
    /// If any standard error is non-finite or non-positive (e.g. a perfectly stable replicate that
    /// underflows to SE=0), we fall back to unweighted rather than silently dropping or boosting that
    /// single point — a zero-weight point would lose its influence, and a 1.0-fallback would treat the
    /// most certain measurement the same as the noisiest.
    /// </summary>
    internal static double[]? BuildVarianceWeights(ComplexityMeasurement[] data)
    {
        if (data.Length == 0) return null;
        if (!data.All(m => m.HasUncertainty)) return null;

        var raw = new double[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            var se = data[i].StandardError;
            if (se <= 0 || !double.IsFinite(se))
                return null;
            raw[i] = 1.0 / (se * se);
        }

        var sum = raw.Sum();
        if (sum <= 0 || !double.IsFinite(sum)) return null;

        var scale = data.Length / sum;
        for (var i = 0; i < raw.Length; i++) raw[i] *= scale;
        return raw;
    }

    public double Predict(int x)
    {
        if (FunctionParameters is null) throw new SailfishModelException("This model has not yet been fit!");
        return Compute(FunctionParameters.Bias, FunctionParameters.Scale, x);
    }

    public override string ToString()
    {
        return FunctionDef
            .Replace("{0}", Math.Round(FunctionParameters?.Scale ?? 0, 4).ToString(CultureInfo.InvariantCulture))
            .Replace("{1}", Math.Round(FunctionParameters?.Bias ?? 0, 4).ToString(CultureInfo.InvariantCulture));
    }
}
