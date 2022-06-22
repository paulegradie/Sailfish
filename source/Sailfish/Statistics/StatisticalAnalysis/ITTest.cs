namespace Sailfish.Statistics.StatisticalAnalysis;

public interface ITTest
{
    TTestResult ExecuteTest(double[] before, double[] after, double alpha = 0.01);
}