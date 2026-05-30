using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Diagnostics.Environment;
using Sailfish.Execution;

namespace Tests.Library.Diagnostics.Environment;

internal static class EnvironmentHealthCheckerTestHelpers
{
    private static readonly Func<HealthCheckEntry> FastTimerProbe = EnvironmentHealthChecker.FastTimerResolutionEntry;
    private static readonly Func<CancellationToken, Task<HealthCheckEntry>> FastCpuProbe =
        static _ => Task.FromResult(EnvironmentHealthChecker.FastBackgroundCpuEntry());

    public static EnvironmentHealthChecker CreateFast(ITimerCalibrationResultProvider? timerProvider = null) =>
        new(timerProvider, FastTimerProbe, FastCpuProbe);
}
