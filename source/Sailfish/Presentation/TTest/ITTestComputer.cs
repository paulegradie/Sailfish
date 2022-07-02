using System.Collections.Generic;
using Sailfish.Statistics.StatisticalAnalysis;

namespace Sailfish.Presentation.TTest;

public interface ITTestComputer
{
    List<NamedTTestResult> ComputeTTest(BeforeAndAfterTrackingFiles beforeAndAfter, TTestSettings settings);
}