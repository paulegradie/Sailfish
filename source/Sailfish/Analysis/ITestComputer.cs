using System.Collections.Generic;
using Sailfish.Contracts.Public;

namespace Sailfish.Analysis;

internal interface ITestComputer
{
    List<TestCaseResults> ComputeTest(TestData beforeTestData, TestData afterTestData, TestSettings settings);
}