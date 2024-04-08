using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Shouldly;
using System.Collections.Generic;
using Tests.Common.Builders;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.ExtensionMethods;

public class TrackingFileDataListExtensionMethodTests
{
    [Fact]
    public void FindsFirstMatchingTestCaseId()
    {
        var displayName = Some.RandomString();
        var caseId = TestCaseIdBuilder.Create().WithTestCaseName(displayName, new List<string>() { "A" }).Build();
        var summaries = new TrackingFileDataList
        {
            new()
            {
                new ClassExecutionSummary(
                    typeof(TrackingFileDataListExtensionMethodTests),
                    new ExecutionSettings(),
                    new[] { new CompiledTestCaseResult(Some.SimpleTestCaseId(), Some.RandomString(), PerformanceRunResultBuilder.Create().Build()) }),
                new ClassExecutionSummary(
                    typeof(TrackingFileDataListExtensionMethodTests),
                    new ExecutionSettings(),
                    new[] { new CompiledTestCaseResult(caseId, Some.RandomString(), PerformanceRunResultBuilder.Create().Build()) }),
            }
        };
        var result = summaries.FindFirstMatchingTestCaseId(caseId);
        if (result?.TestCaseId is null) Assert.Fail();

        result.TestCaseId.DisplayName.ShouldStartWith(displayName);
    }
}