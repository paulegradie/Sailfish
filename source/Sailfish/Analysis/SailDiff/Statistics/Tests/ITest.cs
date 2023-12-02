namespace Sailfish.Analysis.SailDiff.Statistics.Tests;

public interface ITest
{
    TestResultWithOutlierAnalysis ExecuteTest(double[] before, double[] after, SailDiffSettings settings);
}