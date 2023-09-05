using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using MathNet.Numerics;
using Sailfish.Analysis.ScaleFish.CurveFitting;
using Sailfish.Exceptions;

namespace Sailfish.Analysis.ScaleFish;

public abstract class ScaleFishModelFunction : IScaleFishModelFunction
{
    protected ScaleFishModelFunction()
    {
        FitnessCalculator = new FitnessCalculator();
    }

    public abstract string Name { get; set; }
    public abstract string OName { get; set; }
    public abstract string Quality { get; set; }
    public abstract string FunctionDef { get; set; }

    public FittedCurve? FunctionParameters { get; set; }

    // ReSharper disable once MemberCanBePrivate.Global
    [JsonIgnore] public IFitnessCalculator FitnessCalculator { get; set; } // leave this public for testing. Gross, but willing to accept

    public abstract double Compute(double n, double scale, double bias);

    public FitnessResult AnalyzeFitness(IEnumerable<ComplexityMeasurement> validationData)
    {
        var cleanReferenceData = validationData.Where(x => x.Y.IsFinite()).ToArray();
        var xs = cleanReferenceData.Select(x => x.X);

        // 1. fit observations to each of the complexity functions with scale and bias to 'determine' the scale and bias
        // 2. Compute 'standard curve' using new scale and bias for the given function for all Xs
        // 3. Compute RSquared between standard fitted curve and emperical data
        // 4. Choose result with smalled RSquared

        FunctionParameters = FitnessCalculator.CalculateScaleAndBias(cleanReferenceData, Compute);
        var fittedYs = CreateFittedCurveData(xs, FunctionParameters); //.Normalize();
        var observations = cleanReferenceData.Select(x => x.Y).ToArray(); //.Normalize();

        var results = observations
            .Zip(fittedYs)
            .Where(pair => pair.First.IsFinite() && pair.Second.IsFinite())
            .ToList();

        var cleanModeled = results.Select(x => x.First).ToArray();
        var cleanObserved = results.Select(x => x.Second).ToArray();
        try
        {
            var error = FitnessCalculator.CalculateError(cleanModeled, cleanObserved);
            return error;
        }
        catch
        {
            ;
        }

        return new FitnessResult(0, 9999999999);
    }

    public double Predict(int n)
    {
        if (FunctionParameters is null) throw new SailfishModelException("This model has not yet been fit!");
        return Compute(n, FunctionParameters.Scale, FunctionParameters.Bias);
    }

    private IEnumerable<double> CreateFittedCurveData(IEnumerable<double> referenceXs, FittedCurve curveParams)
    {
        return referenceXs.Select(x => Compute(x, curveParams.Scale, curveParams.Bias)).ToArray();
    }

    public override string ToString()
    {
        return FunctionDef
            .Replace("{0}", Math.Round(FunctionParameters?.Scale ?? 0, 4).ToString(CultureInfo.InvariantCulture))
            .Replace("{1}", Math.Round(FunctionParameters?.Bias ?? 0, 4).ToString(CultureInfo.InvariantCulture));
    }
}