using System.Collections.Generic;
using Sailfish.Analysis.ComplexityEstimation.CurveFitting;

namespace Sailfish.Analysis.ComplexityEstimation;

public interface IComplexityFunction
{
    string Name { get; set; }
    string OName { get; set; }
    string Quality { get; set; }

    FitnessResult AnalyzeFitness(IEnumerable<ComplexityMeasurement> referenceData);
}