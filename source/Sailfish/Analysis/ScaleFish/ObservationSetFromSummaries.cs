using System.Collections.Generic;

namespace Sailfish.Analysis.ScaleFish;

public record ObservationSetFromSummaries
{
    public ObservationSetFromSummaries(string TestClassFullName, List<ScaleFishObservation> Observations)
    {
        this.TestClassFullName = TestClassFullName;
        this.Observations = Observations;
    }

    public string TestClassFullName { get; init; }
    public List<ScaleFishObservation> Observations { get; init; }

    public void Deconstruct(out string TestClassFullName, out List<ScaleFishObservation> Observations)
    {
        TestClassFullName = this.TestClassFullName;
        Observations = this.Observations;
    }
}