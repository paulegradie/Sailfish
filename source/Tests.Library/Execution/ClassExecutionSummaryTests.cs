using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Tests.Common.Builders;
using Tests.Common.Utils;
using Tests.E2E.TestSuite.Discoverable.InnerNamespace;
using Xunit;

namespace Tests.Library.Execution;

public class ClassExecutionSummaryTests
{
    private readonly ClassExecutionSummary testSummary;

    public ClassExecutionSummaryTests()
    {
        var successTestCaseId = Some.SimpleTestCaseId();
        var failedTestCaseId = Some.SimpleTestCaseId();

        var perfResult = PerformanceRunResultBuilder.Create().WithDisplayName(successTestCaseId.DisplayName).Build();
        var results = new List<ICompiledTestCaseResult>()
        {
            new CompiledTestCaseResult(successTestCaseId, string.Empty, perfResult),
            new CompiledTestCaseResult(failedTestCaseId, string.Empty, new Exception())
        };

        testSummary = new ClassExecutionSummary(typeof(MinimalTest), new ExecutionSettings(), results);
    }

    [Fact]
    public void GetSuccessfulTestCasesReturnsCasesCorrectly()
    {
        var results = testSummary.GetSuccessfulTestCases().ToList();
        results.Count.ShouldBe(1);
    }

    [Fact]
    public void GetFailedTestCasesReturnsCasesCorrectly()
    {
        var results = testSummary.GetFailedTestCases().ToList();
        results.Count.ShouldBe(1);
    }
}