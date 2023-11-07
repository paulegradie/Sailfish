using System.Collections.Generic;

namespace Sailfish.Analysis.ScaleFish;

public record ObservationSetFromSummaries(string TestClassFullName, List<ScaleFishObservation> Observations);