using Sailfish.Analysis.Saildiff;

namespace Sailfish.Statistics.Tests;

public interface ITest
{
    TestResults ExecuteTest(double[] before, double[] after, TestSettings settings);
}