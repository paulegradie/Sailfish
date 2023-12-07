using Sailfish.Contracts.Public.Models;
using System.Collections.Generic;

namespace Sailfish.Analysis.ScaleFish;

internal record TestCaseComplexityGroup(string TestCaseMethodName, List<ICompiledTestCaseResult> TestCaseGroup);