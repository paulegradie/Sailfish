using System;
using System.Collections.Generic;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Integration;

public class Phase2IntegrationTests
{
    [Fact]
    public void MarkdownTable_IncludesValidationWarnings_ForResults()
    {
        // Arrange: create a PerformanceRunResult with validation warnings
        var warnings = new ValidationResult(new[]
        {
            new ValidationWarning("LOW_SAMPLE_SIZE", "Only 3 effective samples after outlier removal; estimates may be unstable.", ValidationSeverity.Warning),
            new ValidationWarning("ELEVATED_CI", "CI width 30% exceeds budget 20%.", ValidationSeverity.Warning)
        });

        var pr = new PerformanceRunResult(
            displayName: "MyTest",
            mean: 10,
            stdDev: 1,
            variance: 1,
            median: 10,
            rawExecutionResults: new[] { 9.0, 10.0, 11.0, 100.0 },
            sampleSize: 4,
            numWarmupIterations: 0,
            dataWithOutliersRemoved: new[] { 9.0, 10.0, 11.0 },
            upperOutliers: new[] { 100.0 },
            lowerOutliers: Array.Empty<double>(),
            totalNumOutliers: 1,
            standardError: 0.5,
            confidenceLevel: 0.95,
            confidenceIntervalLower: 9,
            confidenceIntervalUpper: 11,
            marginOfError: 1.0
        )
        {
            Validation = warnings
        };

        var compiled = new CompiledTestCaseResult(new TestCaseId("MyTest"), string.Empty, pr);
        var summary = new ClassExecutionSummary(typeof(object), new ExecutionSettings(), new List<ICompiledTestCaseResult> { compiled });

        // Act
        var markdown = new MarkdownTableConverter().ConvertToMarkdownTableString(new[] { summary });

        // Assert
        markdown.ShouldContain("warnings:");
        markdown.ShouldContain("Only 3 effective samples");
        markdown.ShouldContain("CI width");
    }
}

