using Sailfish.Presentation.TTest;

namespace Sailfish.Statistics.StatisticalAnalysis;

internal interface ITTest
{
    TTestResult ExecuteTest(double[] before, double[] after, TTestSettings settings);
}