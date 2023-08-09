using System.Collections.Generic;
using Sailfish.Contracts.Public;

namespace Sailfish.Analysis.Saildiff;

public interface ITestComputer
{
    List<TestCaseResults> ComputeTest(TestData beforeTestData, TestData afterTestData, TestSettings settings);
}