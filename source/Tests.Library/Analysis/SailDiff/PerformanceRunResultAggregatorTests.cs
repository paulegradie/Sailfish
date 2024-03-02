using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using System.Collections.Generic;
using Tests.Library.Execution;
using Tests.Library.Utils;
using Tests.Library.Utils.Builders;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

public class PerformanceRunResultAggregatorTests
{
    [Fact]
    public void AggregatorAggregatesCorrectly()
    {
        var testCaseId = Some.SimpleTestCaseId();
        var perfResults = new List<PerformanceRunResult>()
        {
            PerformanceRunResultBuilder.Create().WithDisplayName(testCaseId.DisplayName).WithRawExecutionResults([1.0, 2, 3]).Build(),
            PerformanceRunResultBuilder.Create().WithDisplayName(testCaseId.DisplayName).WithRawExecutionResults([2.0, 3, 4]).Build(),
        };
        var aggregator = new PerformanceRunResultAggregator();

        var results = aggregator.Aggregate(testCaseId, perfResults);
        results.ShouldNotBeNull();
        results.DisplayName.ShouldBe(testCaseId.DisplayName);
        results.SampleSize.ShouldBe(6);
        results.AggregatedRawExecutionResults.ShouldBeEquivalentTo(new[]
        {
            1.0, 2, 3, 2, 3, 4
        });
    }

    [Fact]
    public void AggregatorReturnsNullWhenNoDataToAggregate()
    {
        var testCaseId = Some.SimpleTestCaseId();
        var aggregator = new PerformanceRunResultAggregator();
        aggregator.Aggregate(testCaseId, new List<PerformanceRunResult>()).ShouldBeNull();
    }
}