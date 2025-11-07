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
    public async Task<EnvironmentHealthReport> CheckAsync(EnvironmentHealthCheckContext? context = null, CancellationToken cancellationToken = default)
    {
        var entries = new List<HealthCheckEntry>
        {
            CheckProcessPriority(),
            CheckGcMode(),
            CheckCpuAffinity(),
            CheckTimerResolution(),
            CheckOsPowerHints()
        };

        // Background CPU load sampling (best-effort)
        try
        {
            var background = await CheckBackgroundCpuLoadAsync().ConfigureAwait(false);
            entries.Add(background);
        }
        catch
        {
            // ignore
        }

        return new EnvironmentHealthReport(entries);
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
            // Use Stopwatch frequency as a proxy for timer resolution
            var freq = Stopwatch.Frequency; // ticks per second
            var isHighRes = Stopwatch.IsHighResolution;
            var resolutionNs = 1_000_000_000.0 / freq;
            var details = isHighRes
                ? $"High-resolution timer: ~{resolutionNs:F0} ns"
                : $"Timer resolution ~{resolutionNs:F0} ns (low resolution)";

            if (isHighRes && resolutionNs <= 200) // ~<=0.2us
            {
                return new("Timer", HealthStatus.Pass, details);
            }

            return new("Timer", HealthStatus.Warn, details, "Ensure high-resolution timers and minimal system timer coalescing");
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
                // Attempt to detect active power scheme via powercfg
                if (TryGetActivePowerScheme(out var scheme))
                {
                    var name = scheme?.ToLowerInvariant() ?? string.Empty;
                    if (name.Contains("ultimate") || name.Contains("high performance") || name.Contains("high-performance"))
                    {
                        return new("Power Plan", HealthStatus.Pass, scheme!);
                    }

                    return new("Power Plan", HealthStatus.Warn, scheme!, "Switch to High/Ultimate Performance to reduce frequency scaling");
                }

                return new("Power Plan", HealthStatus.Unknown, "Not verified", "Run 'powercfg /getactivescheme'");
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

    private static bool TryGetActivePowerScheme(out string? scheme)
    {
        scheme = null;
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "/getactivescheme",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(1000);
            if (string.IsNullOrWhiteSpace(output)) return false;

            // Expected line: "Power Scheme GUID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  (Balanced)"
            var idx = output.IndexOf('(');
            var idx2 = output.IndexOf(')');
            if (idx >= 0 && idx2 > idx)
            {
                scheme = output.Substring(idx + 1, idx2 - idx - 1).Trim();
                return true;
            }

            scheme = output.Trim();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<HealthCheckEntry> CheckBackgroundCpuLoadAsync()
    {
        try
        {
            // Best-effort, dependency-free approximation: measure current process CPU over a short interval
            var p = Process.GetCurrentProcess();
            var startCpu = p.TotalProcessorTime;
            var sw = Stopwatch.StartNew();
            await Task.Delay(500).ConfigureAwait(false);
            p.Refresh();
            var endCpu = p.TotalProcessorTime;
            sw.Stop();

            var cpuMs = (endCpu - startCpu).TotalMilliseconds;
            var elapsedMs = Math.Max(sw.Elapsed.TotalMilliseconds, 1);
            var cpuPct = (cpuMs / (System.Environment.ProcessorCount * elapsedMs)) * 100.0;

            // If our process is already consuming a lot of CPU, background load may be low but measurement will still be noisy.
            // Treat very high process CPU as a warning to close background tasks before benchmarking.
            var status = cpuPct < 20 ? HealthStatus.Pass : cpuPct < 50 ? HealthStatus.Warn : HealthStatus.Fail;
            var rec = status == HealthStatus.Fail ? "Close CPU‑intensive processes and idle the machine before running benchmarks" : null;
            return new("Background CPU (process)", status, $"{cpuPct:F0}%", rec);
        }
        catch (Exception ex)
        {
            return new("Background CPU", HealthStatus.Unknown, ex.Message);
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

