using Sailfish.Statistics.Tests;

namespace Sailfish.Analysis.Saildiff;

public interface IStatisticalTestExecutor
{
    TestResults ExecuteStatisticalTest(double[] beforeData, double[] afterData, TestSettings settings);
}