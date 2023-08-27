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

        var fitnessResults = new List<(ComplexityFunction, FitnessResult)>();
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
            .OrderByDescending(x => x.Item2.RSquared)
            .ToList();

        // sometimes RSquared is about 1 for multiple curves, so we fall back to the RMSE which will be different
        const double tolerance = 1e-6;
        if (Math.Abs(orderedFitnessResults[0].Item2.RSquared - orderedFitnessResults[1].Item2.RSquared) < tolerance)
        {
            // reorder the top 3 to ensure there is no general confusion
            orderedFitnessResults = orderedFitnessResults.Take(3).OrderBy(x => x.Item2.RMSE).ToList();
        }

        var closestComplexity = orderedFitnessResults[0];
        var nextClosestComplexity = orderedFitnessResults[1];

        return new ComplexityResult(
            closestComplexity.Item1,
            closestComplexity.Item2.RSquared,
            nextClosestComplexity.Item1,
            nextClosestComplexity.Item2.RSquared);
    }
}