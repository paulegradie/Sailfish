using System.Collections.Generic;
using Sailfish.Statistics;

namespace Sailfish.Analysis.ScaleFish;

internal record TestCaseComplexityGroup(string TestCaseMethodName, List<ICompiledTestCaseResult> TestCaseGroup);