using Sailfish.Statistics.Tests;

namespace Sailfish.Analysis.SailDiff;

public interface IStatisticalTestExecutor
{
    TestResultWithOutlierAnalysis ExecuteStatisticalTest(double[] beforeData, double[] afterData, SailDiffSettings settings);
}