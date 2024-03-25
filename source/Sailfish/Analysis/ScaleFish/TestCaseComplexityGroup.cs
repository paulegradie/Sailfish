using System.Collections.Generic;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.ScaleFish;

internal record TestCaseComplexityGroup(string TestCaseMethodName, List<ICompiledTestCaseResult> TestCaseGroup);