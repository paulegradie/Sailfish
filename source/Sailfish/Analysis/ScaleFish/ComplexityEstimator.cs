using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Analysis.ScaleFish.CurveFitting;

namespace Sailfish.Analysis.ScaleFish;

public class ComplexityEstimator : IComplexityEstimator
{
    public ScalefishModel EstimateComplexity(ComplexityMeasurement[] measurements)
    {
        var complexityFunctions = ComplexityReferences.GetComplexityFunctions();

        var fitnessResults = new List<(ScaleFishModelFunction, FitnessResult)>();
        foreach (var complexityFunction in complexityFunctions)
        {
            try
            {
                var fitness = complexityFunction.AnalyzeFitness(measurements);
                fitnessResults.Add((complexityFunction, fitness));
            }
            catch
            {
                // ignore this complexity - too difficult to converge - find the next best curve to explain the data
            }
        }

        var orderedFitnessResults = fitnessResults
            .Where(x => x.Item2.IsValid)
            .OrderByDescending(x => x.Item2.Ssd)
            .ToList();

        var closestComplexity = orderedFitnessResults[0];
        var nextClosestComplexity = orderedFitnessResults[1];

        return new ScalefishModel(
            closestComplexity.Item1,
            closestComplexity.Item2.RSquared,
            nextClosestComplexity.Item1,
            nextClosestComplexity.Item2.RSquared);
    }
}