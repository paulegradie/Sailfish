using Sailfish.Presentation.TTest;

namespace Sailfish.Statistics.StatisticalAnalysis;

public interface ITTest
{
    TTestResult ExecuteTest(double[] before, double[] after, TTestSettings settings);
}