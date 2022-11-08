using Sailfish.Statistics.Tests;

namespace Sailfish.Analysis;

public interface IStatisticalTestExecutor
{
    TestResults ExecuteStatisticalTest(double[] beforeData, double[] afterData, TestSettings settings);
}