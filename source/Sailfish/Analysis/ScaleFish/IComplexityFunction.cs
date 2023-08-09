using System.Collections.Generic;
using Sailfish.Analysis.Scalefish.CurveFitting;

namespace Sailfish.Analysis.Scalefish;

public interface IComplexityFunction
{
    string Name { get; set; }
    string OName { get; set; }
    string Quality { get; set; }

    FitnessResult AnalyzeFitness(IEnumerable<ComplexityMeasurement> referenceData);
}