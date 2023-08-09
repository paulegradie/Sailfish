using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Analysis.Scalefish.CurveFitting;

namespace Sailfish.Analysis.Scalefish;

public class ComplexityEstimator : IComplexityEstimator
{
    public ComplexityResult EstimateComplexity(ComplexityMeasurement[] measurements)
    {
        var complexityFunctions = ComplexityReferences.GetComplexityFunctions();

        var fitnessResults = new List<(IComplexityFunction, FitnessResult)>();
        foreach (var complexityFunction in complexityFunctions)
        {
            try
            {
                var fitness = complexityFunction.AnalyzeFitness(measurements);
                fitnessResults.Add((complexityFunction, fitness));
            }
            catch (Exception ex)
            {
                // ignore this complexity - too difficult to converge - find the next best curve to explain the data
            }
        }

        var orderedFitnessResults = fitnessResults
            .OrderByDescending(x => x.Item2.RSquared)
            .ToList();

        var closestComplexity = orderedFitnessResults[0];
        var nextClosestComplexity = orderedFitnessResults[1];

        return new ComplexityResult(
            closestComplexity.Item1,
            closestComplexity.Item2.RSquared,
            nextClosestComplexity.Item1,
            nextClosestComplexity.Item2.RSquared);
    }
}