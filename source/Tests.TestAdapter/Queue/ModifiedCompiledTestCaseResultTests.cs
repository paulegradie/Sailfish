using System;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.TestAdapter.Queue.Processors.MethodComparison;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Queue;

public class ModifiedCompiledTestCaseResultTests
{
    [Fact]
    public void Constructor_And_PropertyDelegation_Works()
    {
        // Arrange
        var original = Substitute.For<ICompiledTestCaseResult>();
        var originalTestCaseId = new TestCaseId("MyClass.MyMethod");
        original.TestCaseId.Returns(originalTestCaseId);
        original.GroupingId.Returns("GroupA");
        var originalException = new InvalidOperationException("boom");
        original.Exception.Returns(originalException);

        var modified = new PerformanceRunResult(
            displayName: "CommonId",
            mean: 1.23,
            stdDev: 0.1,
            variance: 0.01,
            median: 1.20,
            rawExecutionResults: Array.Empty<double>(),
            sampleSize: 10,
            numWarmupIterations: 0,
            dataWithOutliersRemoved: Array.Empty<double>(),
            upperOutliers: Array.Empty<double>(),
            lowerOutliers: Array.Empty<double>(),
            totalNumOutliers: 0);

        // Act
        var wrapper = new ModifiedCompiledTestCaseResult(original, modified);

        // Assert
        wrapper.TestCaseId.ShouldBe(originalTestCaseId);
        wrapper.GroupingId.ShouldBe("GroupA");
        wrapper.Exception.ShouldBe(originalException);
        wrapper.PerformanceRunResult.ShouldBeSameAs(modified);
    }
}