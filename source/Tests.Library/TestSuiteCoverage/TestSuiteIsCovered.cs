using System;
using System.Collections.Generic;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Tests.Common.Builders;
using Tests.Common.Builders.ScaleFish;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.TestSuiteCoverage;

public class TestSuiteIsCovered
{
    [Fact]
    public void TestBuildersDontBlowUp()
    {
        var variables = TestCaseVariablesBuilder.Create().AddVariable(new TestCaseVariable(Some.RandomString(), new { })).AddVariable(Some.RandomString(), new { }).Build();
        var testCaseId = TestCaseIdBuilder.Create().WithTestCaseName(Some.RandomString()).WithTestCaseVariables(new List<TestCaseVariable>() { }).Build();
        var testCaseName = TestCaseNameBuilder.Create().WithName(Some.RandomString()).WithParts(new List<string>() { Some.RandomString() }).Build();

        var perfRunResultTrackingFormat = PerformanceRunResultTrackingFormatBuilder.Create()
            .WithDisplayName(Some.RandomString())
            .WithMean(0.1)
            .WithMedian(0.2)
            .WithStdDev(1.0)
            .WithVariance(234.0)
            .WithRawExecutionResults([1.0, 2.0])
            .WithSampleSize(2)
            .WithDataWithOutliersRemoved([3.0, 2.0])
            .WithNumWarmupIterations(3)
            .WithLowerOutliers([2, 3])
            .WithUpperOutliers([3, 4])
            .WithTotalNumOutliers(4)
            .Build();

        var perfRunResult = PerformanceRunResultBuilder.Create()
            .WithMean(2.0)
            .WithStdDev(3.0)
            .WithVariance(2345.0)
            .WithMedian(2.3)
            .WithSampleSize(3)
            .WithNumWarmupIterations(2)
            .WithDataWithOutliersRemoved([1.0, 2.0])
            .WithLowerOutliers([1.2, 3.0])
            .WithUpperOutliers([2.3, 4.5])
            .WithTotalNumOutliers(4)
            .Build();

        var executionSettings = ExecutionSettingsTrackingFormatBuilder.Create()
            .WithAsCsv(true)
            .WithAsConsole(true)
            .WithAsMarkdown(true)
            .WithNumWarmupIterations(2)
            .WithSampleSize(3)
            .WithDisableOverheadEstimation(true)
            .Build();

        // Assert - Verify all builders created valid objects
        variables.ShouldNotBeNull();
        testCaseId.ShouldNotBeNull();
        testCaseName.ShouldNotBeNull();
        perfRunResultTrackingFormat.ShouldNotBeNull();
        perfRunResult.ShouldNotBeNull();
        executionSettings.ShouldNotBeNull();

        CompiledTestCaseResultTrackingFormatBuilder.Create()
            .WithGroupingId(Some.RandomString())
            .WithPerformanceRunResult(perfRunResultTrackingFormat)
            .WithException(new Exception())
            .WithTestCaseId(testCaseId)
            .Build();

        ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestSuiteIsCovered))
            .WithExecutionSettings(executionSettings)
            .WithExecutionSettings(b => b.Build())
            .WithCompiledTestCaseResult([])
            .WithCompiledTestCaseResult(b => b.Build())
            .Build();

        var scaleFishModel = ScaleFishModelBuilder.Create()
            .AddPrimaryFunction(new Exponential())
            .SetPrimaryGoodnessOfFit(2.0)
            .AddSecondaryFunction(new Cubic())
            .SetSecondaryGoodnessOfFit(4.0)
            .Build();

        ScaleFishPropertyModelBuilder.Create()
            .WithPropertyName(Some.RandomString())
            .WithScaleFishModel(scaleFishModel)
            .WithScaleFishModel(b => b.Build())
            .Build();
    }
}
