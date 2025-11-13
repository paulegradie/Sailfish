using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Sailfish.Execution;

namespace Tests.Library.Execution;

public class HarnessBaselineCalibratorTests
{
    private class ProbeTargets
    {
        public void Sync() { }
        public void SyncToken(CancellationToken _) { }
        public Task Async() => Task.CompletedTask;
        public Task AsyncToken(CancellationToken _) => Task.CompletedTask;
    }

    private static MethodInfo M(string name) => typeof(ProbeTargets).GetMethod(name)!;

    [Fact]
    public async Task Calibrate_Sync_ShouldReturnNonNegative()
    {
        var cal = new HarnessBaselineCalibrator();
        var result = await cal.CalibrateTicksAsync(M(nameof(ProbeTargets.Sync)), CancellationToken.None);
        result.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Calibrate_SyncToken_ShouldReturnNonNegative()
    {
        var cal = new HarnessBaselineCalibrator();
        var result = await cal.CalibrateTicksAsync(M(nameof(ProbeTargets.SyncToken)), CancellationToken.None);
        result.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Calibrate_Async_ShouldReturnNonNegative()
    {
        var cal = new HarnessBaselineCalibrator();
        var result = await cal.CalibrateTicksAsync(M(nameof(ProbeTargets.Async)), CancellationToken.None);
        result.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Calibrate_AsyncToken_ShouldReturnNonNegative()
    {
        var cal = new HarnessBaselineCalibrator();
        var result = await cal.CalibrateTicksAsync(M(nameof(ProbeTargets.AsyncToken)), CancellationToken.None);
        result.ShouldBeGreaterThanOrEqualTo(0);
    }
}

