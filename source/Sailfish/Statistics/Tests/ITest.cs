using Sailfish.Analysis.SailDiff;

namespace Sailfish.Statistics.Tests;

public interface ITest
{
    TestResultWithOutlierAnalysis ExecuteTest(double[] before, double[] after, SailDiffSettings settings);
}