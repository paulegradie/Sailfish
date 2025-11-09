using System;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class PrecisionTimeBudgetControllerTests
{
    [Fact]
    public void Adjust_TightBudget_RelaxesThresholds()
    {
        var controller = new PrecisionTimeBudgetController();
        var settings = new ExecutionSettings
        {
            UseTimeBudgetController = true,
            TargetCoefficientOfVariation = 0.05,
            MaxConfidenceIntervalWidth = 0.20,
            MaxMeasurementTimePerMethod = TimeSpan.FromMilliseconds(25)
        };

        // Pilot samples ~5ms each
        var pilot = new double[] { 5_000_000, 5_000_000, 5_000_000 };
        var start = DateTimeOffset.Now - TimeSpan.FromMilliseconds(10);
        var now = DateTimeOffset.Now;

        var adjusted = controller.Adjust(pilot, settings, start, now);

        adjusted.TargetCV.ShouldBeGreaterThanOrEqualTo(0.05);
        adjusted.MaxConfidenceIntervalWidth.ShouldBeGreaterThanOrEqualTo(0.20);
        (adjusted.TargetCV > 0.05 || adjusted.MaxConfidenceIntervalWidth > 0.20).ShouldBeTrue();
    }

    [Fact]
    public void Adjust_GenerousBudget_DoesNotChangeThresholds()
    {
        var controller = new PrecisionTimeBudgetController();
        var settings = new ExecutionSettings
        {
            UseTimeBudgetController = true,
            TargetCoefficientOfVariation = 0.05,
            MaxConfidenceIntervalWidth = 0.20,
            MaxMeasurementTimePerMethod = TimeSpan.FromMilliseconds(500)
        };

        var pilot = new double[] { 5_000_000, 5_000_000, 5_000_000 };
        var start = DateTimeOffset.Now - TimeSpan.FromMilliseconds(10);
        var now = DateTimeOffset.Now;

        var adjusted = controller.Adjust(pilot, settings, start, now);

        adjusted.TargetCV.ShouldBe(0.05, 1e-9);
        adjusted.MaxConfidenceIntervalWidth.ShouldBe(0.20, 1e-9);
    }
}