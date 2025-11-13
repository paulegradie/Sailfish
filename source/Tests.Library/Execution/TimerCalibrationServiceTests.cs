using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class TimerCalibrationServiceTests
{
    [Fact]
    public async Task CalibrateAsync_Returns_SensibleRanges()
    {
        // Arrange: instantiate internal TimerCalibrationService via reflection
        var serviceType = typeof(MarkdownTableConverter).Assembly.GetType("Sailfish.Execution.TimerCalibrationService")
                          ?? Type.GetType("Sailfish.Execution.TimerCalibrationService, Sailfish", throwOnError: true)!;
        var service = Activator.CreateInstance(serviceType, nonPublic: true)!;
        var method = serviceType.GetMethod("CalibrateAsync", BindingFlags.Public | BindingFlags.Instance)!;

        // Act
        var taskObj = method.Invoke(service, new object?[] { CancellationToken.None })!;
        // Await Task<T> then read Result via reflection
        await ((Task)taskObj);
        var resultObj = taskObj.GetType().GetProperty("Result")!.GetValue(taskObj)!;
        var result = (TimerCalibrationResult)resultObj;

        // Assert
        result.StopwatchFrequency.ShouldBeGreaterThan(0);
        result.ResolutionNs.ShouldBeGreaterThan(0);
        result.Warmups.ShouldBe(16);
        result.Samples.ShouldBe(64);
        result.BaselineOverheadTicks.ShouldBeGreaterThanOrEqualTo(0);
        result.RsdPercent.ShouldBeGreaterThanOrEqualTo(0);
        result.JitterScore.ShouldBeInRange(0, 100);
    }
}