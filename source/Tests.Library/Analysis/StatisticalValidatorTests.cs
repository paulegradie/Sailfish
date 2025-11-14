using Sailfish.Analysis;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis;

public class StatisticalValidatorTests
{
    private readonly StatisticalValidator _validator = new();

    [Fact]
    public void Validate_LowSampleSize_Warns()
    {
        var pr = new PerformanceRunResult(
            displayName: "t",
            mean: 10,
            stdDev: 1,
            variance: 1,
            median: 10,
            rawExecutionResults: [9,10,11,10,10,9,11],
            sampleSize: 7,
            numWarmupIterations: 0,
            dataWithOutliersRemoved: [9,10,11,10,10],
            upperOutliers: [],
            lowerOutliers: [],
            totalNumOutliers: 2,
            standardError: 0.5,
            confidenceLevel: 0.95,
            confidenceIntervalLower: 9,
            confidenceIntervalUpper: 11,
            marginOfError: 1.0);

        var settings = new ExecutionSettings { MinimumSampleSize = 10, TargetCoefficientOfVariation = 0.05, MaxConfidenceIntervalWidth = 0.2 };
        var result = _validator.Validate(pr, settings);
        result.HasWarnings.ShouldBeTrue();
        result.Warnings.ShouldContain(w => w.Code == "LOW_SAMPLE_SIZE");
    }

    [Fact]
    public void Validate_ExcessiveOutliers_Critical()
    {
        var pr = new PerformanceRunResult(
            displayName: "t",
            mean: 10,
            stdDev: 1,
            variance: 1,
            median: 10,
            rawExecutionResults: [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20],
            sampleSize: 20,
            numWarmupIterations: 0,
            dataWithOutliersRemoved: [5,6,7,8,9,10,11,12,13,14,15,16,17,18],
            upperOutliers: [],
            lowerOutliers: [],
            totalNumOutliers: 6,
            standardError: 0.1,
            confidenceLevel: 0.95,
            confidenceIntervalLower: 9.5,
            confidenceIntervalUpper: 10.5,
            marginOfError: 0.5);

        var settings = new ExecutionSettings { TargetCoefficientOfVariation = 0.05, MaxConfidenceIntervalWidth = 0.2 };
        var result = _validator.Validate(pr, settings);
        result.Warnings.ShouldContain(w => w.Code == "EXCESSIVE_OUTLIERS" && w.Severity == ValidationSeverity.Critical);
    }

    [Fact]
    public void Validate_HighCv_Warns()
    {
        var pr = new PerformanceRunResult(
            displayName: "t",
            mean: 10,
            stdDev: 3,
            variance: 9,
            median: 10,
            rawExecutionResults: [7,8,9,10,11,12,13],
            sampleSize: 7,
            numWarmupIterations: 0,
            dataWithOutliersRemoved: [7,8,9,10,11,12,13],
            upperOutliers: [],
            lowerOutliers: [],
            totalNumOutliers: 0,
            standardError: 1.0,
            confidenceLevel: 0.95,
            confidenceIntervalLower: 8,
            confidenceIntervalUpper: 12,
            marginOfError: 2.0);

        var settings = new ExecutionSettings { TargetCoefficientOfVariation = 0.05, MaxConfidenceIntervalWidth = 0.5 };
        var result = _validator.Validate(pr, settings);
        result.Warnings.ShouldContain(w => w.Code == "HIGH_CV" || w.Code == "ELEVATED_CV");
    }

    [Fact]
    public void Validate_WideCI_Warns()
    {
        var pr = new PerformanceRunResult(
            displayName: "t",
            mean: 100,
            stdDev: 10,
            variance: 100,
            median: 100,
            rawExecutionResults: [90,100,110],
            sampleSize: 3,
            numWarmupIterations: 0,
            dataWithOutliersRemoved: [90,100,110],
            upperOutliers: [],
            lowerOutliers: [],
            totalNumOutliers: 0,
            standardError: 5.0,
            confidenceLevel: 0.95,
            confidenceIntervalLower: 80,
            confidenceIntervalUpper: 120,
            marginOfError: 20.0);

        var settings = new ExecutionSettings { TargetCoefficientOfVariation = 0.05, MaxConfidenceIntervalWidth = 0.2 };
        var result = _validator.Validate(pr, settings);
        result.Warnings.ShouldContain(w => w.Code == "WIDE_CI" || w.Code == "ELEVATED_CI");
    }
}

