using System.Collections.Generic;
using Sailfish.Analysis.ScaleFish.CurveFitting;

namespace Sailfish.Analysis.ScaleFish;

public interface IScaleFishModelFunction
{
    string Name { get; set; }
    string OName { get; set; }
    string Quality { get; set; }
    public FittedCurve? FunctionParameters { get; set; }

    FitnessResult AnalyzeFitness(IEnumerable<ComplexityMeasurement> validationData);
    public double Predict(int n);
}