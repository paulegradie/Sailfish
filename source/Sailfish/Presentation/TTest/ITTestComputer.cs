using System.Collections.Generic;
using Sailfish.Contracts.Public;

namespace Sailfish.Presentation.TTest;

internal interface ITTestComputer
{
    List<NamedTTestResult> ComputeTTest(TestData beforeTestData, TestData afterTestData, TTestSettings settings);
}