using Shouldly;
using Xunit;
using Sailfish.Execution;

namespace Tests.Library.Execution;

public class ExecutionSettingsTests
{
    [Fact]
    public void DefaultConstructor_SetsExpectedDefaults()
    {
        var settings = new ExecutionSettings();

        settings.AsCsv.ShouldBeFalse();
        settings.AsConsole.ShouldBeFalse();
        settings.AsMarkdown.ShouldBeFalse();

        settings.UseAdaptiveSampling.ShouldBeFalse();
        settings.TargetCoefficientOfVariation.ShouldBe(0.05);
        settings.MinimumSampleSize.ShouldBe(10);
        settings.MaximumSampleSize.ShouldBe(1000);
        settings.ConfidenceLevel.ShouldBe(0.95);
        settings.ReportConfidenceLevels.ShouldContain(0.95);
        settings.ReportConfidenceLevels.ShouldContain(0.99);
        settings.MaxConfidenceIntervalWidth.ShouldBe(0.20);
        settings.UseRelativeConfidenceInterval.ShouldBeTrue();
    }

    [Fact]
    public void ParameterizedConstructor_SetsFlagsAndCounts()
    {
        var settings = new ExecutionSettings(asCsv: true, asConsole: false, asMarkdown: true, sampleSize: 12, numWarmupIterations: 3);

        settings.AsCsv.ShouldBeTrue();
        settings.AsConsole.ShouldBeFalse();
        settings.AsMarkdown.ShouldBeTrue();
        settings.SampleSize.ShouldBe(12);
        settings.NumWarmupIterations.ShouldBe(3);

        // Defaults remain intact
        settings.UseAdaptiveSampling.ShouldBeFalse();
        settings.TargetCoefficientOfVariation.ShouldBe(0.05);
    }
}

