using System.Collections.Generic;
using Sailfish.Analysis.ScaleFish.CurveFitting;

namespace Sailfish.Analysis.ScaleFish;

public interface IComplexityFunction
{
    string Name { get; set; }
    string OName { get; set; }
    string Quality { get; set; }
    public FittedCurve? FunctionParameters { get; set; }

    FitnessResult AnalyzeFitness(IEnumerable<ComplexityMeasurement> referenceData);

    public double Predict(int n);
}