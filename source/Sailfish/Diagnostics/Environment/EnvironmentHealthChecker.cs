using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Diagnostics.Environment;

public class EnvironmentHealthChecker : IEnvironmentHealthChecker
{
    public Task<EnvironmentHealthReport> CheckAsync(EnvironmentHealthCheckContext? context = null, CancellationToken cancellationToken = default)
    {
        var entries = new List<HealthCheckEntry>
        {
            CheckProcessPriority(),
            CheckGcMode(),
            CheckCpuAffinity(),
            CheckTimerResolution(),
            CheckOsPowerHints()
        };

        return Task.FromResult(new EnvironmentHealthReport(entries));
    }

    private static HealthCheckEntry CheckProcessPriority()
    {
        try
        {
            var p = Process.GetCurrentProcess();
            var cls = p.PriorityClass;
            return cls switch
            {
                ProcessPriorityClass.RealTime or ProcessPriorityClass.High or ProcessPriorityClass.AboveNormal
                    => new("Process Priority", HealthStatus.Pass, $"{cls}", "Optional: Set High for maximum isolation"),
                ProcessPriorityClass.Normal
                    => new("Process Priority", HealthStatus.Warn, $"{cls}", "Consider High or AboveNormal to reduce scheduler noise"),
                _ => new("Process Priority", HealthStatus.Warn, $"{cls}", "Consider High or AboveNormal to reduce scheduler noise")
            };
        }
        catch (Exception ex)
        {
            return new("Process Priority", HealthStatus.Unknown, ex.Message);
        }
    }

    private static HealthCheckEntry CheckGcMode()
    {
        try
        {
            var isServer = System.Runtime.GCSettings.IsServerGC;
            return isServer
                ? new("GC Mode", HealthStatus.Pass, "Server GC enabled")
                : new("GC Mode", HealthStatus.Warn, "Workstation GC", "Enable Server GC for more stable throughput measurements");
        }
        catch (Exception ex)
        {
            return new("GC Mode", HealthStatus.Unknown, ex.Message);
        }
    }

    private static HealthCheckEntry CheckCpuAffinity()
    {
        try
        {
            var p = Process.GetCurrentProcess();
            var mask = (ulong)p.ProcessorAffinity;
            var bits = CountBits(mask);
            return bits switch
            {
                0 => new("CPU Affinity", HealthStatus.Unknown, "Affinity mask empty"),
                1 => new("CPU Affinity", HealthStatus.Pass, "Pinned to a single core"),
                >= 2 and <= 4 => new("CPU Affinity", HealthStatus.Warn, $"Pinned to {bits} cores", "Pin to 1 core to minimize cross-core jitter"),
                _ => new("CPU Affinity", HealthStatus.Warn, "All cores", "Pin to 1 core to minimize cross-core jitter")
            };
        }
        catch (Exception ex)
        {
            return new("CPU Affinity", HealthStatus.Unknown, ex.Message);
        }
    }

    private static HealthCheckEntry CheckTimerResolution()
    {
        try
        {
            // Heuristic: High-resolution timer availability differs across platforms. If Windows, assume high-res if QueryPerformanceCounter exists.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // .NET uses QPC under the hood; we can't query resolution directly without interop.
                return new("Timer", HealthStatus.Pass, "High-resolution timers available (QPC)");
            }
            return new("Timer", HealthStatus.Unknown, "Resolution not verified on this platform");
        }
        catch (Exception ex)
        {
            return new("Timer", HealthStatus.Unknown, ex.Message);
        }
    }

    private static HealthCheckEntry CheckOsPowerHints()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // We are not invoking powercfg. Provide a conservative hint.
                return new("Power Plan", HealthStatus.Warn, "Not verified", "Use High Performance / Ultimate Performance plan");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new("Power Management", HealthStatus.Warn, "Not verified", "Disable App Nap and set Energy Saver to prevent sleep");
            }

            return new("Power Management", HealthStatus.Unknown, "Not verified");
        }
        catch (Exception ex)
        {
            return new("Power Management", HealthStatus.Unknown, ex.Message);
        }
    }

    private static int CountBits(ulong v)
    {
        var count = 0;
        while (v != 0)
        {
            v &= v - 1;
            count++;
        }
        return count;
    }
}

