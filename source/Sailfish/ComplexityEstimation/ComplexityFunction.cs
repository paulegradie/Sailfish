using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.ComplexityEstimation;

public abstract class ComplexityFunction : IComplexityFunction
{
    public abstract string Name { get; set; }
    public abstract string OName { get; set; }
    public abstract string Quality { get; set; }

    public abstract double Compute(int n);

    private double[] CreateExampleData(IEnumerable<int> referenceXs)
    {
        return referenceXs.Select(Compute).ToArray();
    }

    public double ComputeError(ComplexityMeasurement[] referenceData)
    {
        var xs = referenceData.Select(x => x.X);
        var theoreticalYs = CreateExampleData(xs).Normalize();
        var empericalYs = referenceData.Select(x => x.Y).ToArray().Normalize();

        var squaredError = empericalYs.Zip(theoreticalYs).Select(pair => Math.Pow(pair.First - pair.Second, 2)).Sum();
        return squaredError;
    }
}