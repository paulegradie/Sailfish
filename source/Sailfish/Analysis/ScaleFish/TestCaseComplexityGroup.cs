using System.Collections.Generic;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.ScaleFish;

internal record TestCaseComplexityGroup
{
    public TestCaseComplexityGroup(string TestCaseMethodName, List<ICompiledTestCaseResult> TestCaseGroup)
    {
        this.TestCaseMethodName = TestCaseMethodName;
        this.TestCaseGroup = TestCaseGroup;
    }

    public string TestCaseMethodName { get; init; }
    public List<ICompiledTestCaseResult> TestCaseGroup { get; init; }

    public void Deconstruct(out string TestCaseMethodName, out List<ICompiledTestCaseResult> TestCaseGroup)
    {
        TestCaseMethodName = this.TestCaseMethodName;
        TestCaseGroup = this.TestCaseGroup;
    }
}