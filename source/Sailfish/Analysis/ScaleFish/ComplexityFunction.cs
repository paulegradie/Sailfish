using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using Sailfish.Analysis.Scalefish.CurveFitting;

namespace Sailfish.Analysis.Scalefish;

public abstract class ComplexityFunction : IComplexityFunction
{
    private readonly IFitnessCalculator fitnessCalculator;

    protected ComplexityFunction(IFitnessCalculator fitnessCalculator)
    {
        this.fitnessCalculator = fitnessCalculator;
    }

    public abstract string Name { get; set; }
    public abstract string OName { get; set; }
    public abstract string Quality { get; set; }
    public FittedCurve FunctionParameters { get; set; }

    public abstract double Compute(double n, double scale, double bias);

    public FitnessResult AnalyzeFitness(IEnumerable<ComplexityMeasurement> referenceData)
    {
        var cleanReferenceData = referenceData.Where(x => x.Y.IsFinite()).ToArray();
        var xs = cleanReferenceData.Select(x => x.X);

        // 1. fit observations to each of the complexity functions with scale and bias to 'determine' the scale and bias
        // 2. Compute 'standard curve' using new scale and bias for the given function for all Xs
        // 3. Compute RSquared between standard fitted curve and emperical data
        // 4. Choose result with smalled RSquared

        FunctionParameters  = fitnessCalculator.CalculateScaleAndBias(cleanReferenceData, Compute);
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
            var error = fitnessCalculator.CalculateError(cleanModeled, cleanObserved);
            return error;
        }
        catch
        {
            ;
        }

        return new FitnessResult(0, 9999999999);
    }

    private IEnumerable<double> CreateFittedCurveData(IEnumerable<double> referenceXs, FittedCurve curveParams)
    {
        return referenceXs.Select(x => Compute(x, curveParams.Scale, curveParams.Bias)).ToArray();
    }
}