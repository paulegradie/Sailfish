using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;

namespace Sailfish.ComplexityEstimation;

public abstract class ComplexityFunction : IComplexityFunction
{
    public abstract string Name { get; set; }
    public abstract string OName { get; set; }
    public abstract string Quality { get; set; }

    public abstract double Compute(int n);

    protected double[] CreateExampleData(IEnumerable<int> referenceXs)
    {
        return referenceXs.Select(Compute).ToArray();
    }

    public virtual double ComputeError(ComplexityMeasurement[] referenceData)
    {
        var cleanReferenceData = referenceData.Where(x => x.Y.IsFinite()).ToArray();
        var xs = cleanReferenceData.Select(x => x.X);
        var theoreticalYs = CreateExampleData(xs).Normalize();
        var empericalYs = cleanReferenceData.Select(x => x.Y).ToArray().Normalize();
        var squaredError = empericalYs
            .Zip(theoreticalYs)
            .Where(pair => pair.First.IsFinite() && pair.Second.IsFinite())
            .Select(pair => Math.Pow(pair.First - pair.Second, 2))
            .Sum();
        return squaredError;
    }
}