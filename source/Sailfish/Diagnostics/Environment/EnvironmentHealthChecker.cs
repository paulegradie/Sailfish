using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Diagnostics.Environment;

public class EnvironmentHealthChecker : IEnvironmentHealthChecker
{
    private readonly ITimerCalibrationResultProvider? _timerProvider;
    private readonly Func<HealthCheckEntry> _timerResolutionProbe;
    private readonly Func<CancellationToken, Task<HealthCheckEntry>> _backgroundCpuProbe;

    public EnvironmentHealthChecker()
        : this(timerProvider: null, timerResolutionProbe: null, backgroundCpuProbe: null)
    {
    }

    public EnvironmentHealthChecker(ITimerCalibrationResultProvider timerProvider)
        : this(timerProvider, timerResolutionProbe: null, backgroundCpuProbe: null)
    {
    }

    internal EnvironmentHealthChecker(
        ITimerCalibrationResultProvider? timerProvider,
        Func<HealthCheckEntry>? timerResolutionProbe,
        Func<CancellationToken, Task<HealthCheckEntry>>? backgroundCpuProbe)
    {
        _timerProvider = timerProvider;
        _timerResolutionProbe = timerResolutionProbe ?? CheckTimerResolution;
        _backgroundCpuProbe = backgroundCpuProbe ?? (static ct => CheckBackgroundCpuLoadAsync(ct));
    }

    public async Task<EnvironmentHealthReport> CheckAsync(EnvironmentHealthCheckContext? context = null, CancellationToken cancellationToken = default)
    {
        var entries = new List<HealthCheckEntry>
        {
            CheckBuildConfiguration(context),
            CheckJitSettings(),
            CheckProcessPriority(),
            CheckGcMode(),
            CheckCpuAffinity(),
            _timerResolutionProbe(),
            CheckOsPowerHints()
        };

        // If we have timer calibration results, include Timer Jitter entry
        try
        {
            var jitter = CheckTimerJitterFromCalibration(_timerProvider);
            if (jitter is not null) entries.Add(jitter);
        }
        catch { /* best-effort */ }

        // Background CPU load sampling (best-effort)
        try
        {
            var background = await _backgroundCpuProbe(cancellationToken).ConfigureAwait(false);
            entries.Add(background);
        }
        catch
        {
            // ignore
        }

        return new EnvironmentHealthReport(entries);
    }

    internal static HealthCheckEntry FastTimerResolutionEntry() =>
        new("Timer", HealthStatus.Pass, "Stubbed for fast tests");

    internal static HealthCheckEntry FastBackgroundCpuEntry() =>
        new("Background CPU", HealthStatus.Pass, "Stubbed for fast tests");

    private static HealthCheckEntry CheckBuildConfiguration(EnvironmentHealthCheckContext? context)
    {
        try
        {
            // Prefer the test assembly (represents user code) if provided; otherwise fall back to entry/executing assembly
            var asm = default(Assembly);
            var path = context?.TestAssemblyPath;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                asm = Assembly.LoadFrom(path);
            }
            else
            {
                asm = Assembly.GetEntryAssembly() ?? typeof(EnvironmentHealthChecker).Assembly;
            }

            var dbg = asm.GetCustomAttribute<DebuggableAttribute>();
            var isDebug = dbg != null && (dbg.DebuggingFlags & DebuggableAttribute.DebuggingModes.DisableOptimizations) != 0;
            if (isDebug)
            {
                return new("Build Mode", HealthStatus.Warn, "Debug", "Use Release (optimized) for stable measurements");
            }

            return new("Build Mode", HealthStatus.Pass, "Release");
        }
        catch (Exception ex)
        {
            return new("Build Mode", HealthStatus.Unknown, ex.Message);
        }
    }

    private static HealthCheckEntry CheckJitSettings()
    {
        try
        {
            static string ReadFlag(string name)
            {
                var v = System.Environment.GetEnvironmentVariable(name);
                return string.IsNullOrWhiteSpace(v) ? "default" : v.Trim();
            }

            var tiered = ReadFlag("COMPlus_TieredCompilation");
            var quickJit = ReadFlag("COMPlus_TC_QuickJit");
            var quickJitLoops = ReadFlag("COMPlus_TC_QuickJitForLoops");
            var osr = ReadFlag("COMPlus_TC_OnStackReplacement");

            var details = $"Tiered={tiered}; QuickJit={quickJit}; QuickJitForLoops={quickJitLoops}; OSR={osr}";

            // If TieredCompilation is explicitly disabled, warn; otherwise pass (defaults generally enable tiering)
            if (string.Equals(tiered, "0", StringComparison.OrdinalIgnoreCase) || string.Equals(tiered, "false", StringComparison.OrdinalIgnoreCase))
            {
                return new("JIT (Tiered/OSR)", HealthStatus.Warn, details, "Enable Tiered JIT for representative steady-state performance");
            }

            return new("JIT (Tiered/OSR)", HealthStatus.Pass, details);
        }
        catch (Exception ex)
        {
            return new("JIT (Tiered/OSR)", HealthStatus.Unknown, ex.Message);
        }
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
            if (!(OperatingSystem.IsWindows() || OperatingSystem.IsLinux()))
            {
                return new("CPU Affinity", HealthStatus.Unknown, "Not supported on this OS");
            }

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
            // 1) High-resolution performance counter (Stopwatch)
            var freq = Stopwatch.Frequency; // ticks per second
            var isHighRes = Stopwatch.IsHighResolution;
            var reportedResolutionNs = 1_000_000_000.0 / freq; // what the API advertises: one tick

            // 1b) EFFECTIVE resolution. Stopwatch.Frequency only states the tick UNIT, not how often
            // the counter actually advances. On several platforms the advertised value is far finer
            // than reality — e.g. Apple Silicon reports Frequency == 1e9 ("1 ns/tick") while the
            // hardware timebase only advances every ~41.7 ns (24 MHz). Probe the smallest non-zero
            // GetTimestamp() delta to recover the true granularity. We take the MIN because scheduler
            // hiccups only ever inflate a delta and so cannot mask the real quantum.
            double effectiveResolutionNs;
            try
            {
                effectiveResolutionNs = MeasureEffectiveResolutionNs(freq);
            }
            catch
            {
                effectiveResolutionNs = double.NaN; // best effort only
            }

            var haveEffective = !double.IsNaN(effectiveResolutionNs) && effectiveResolutionNs > 0;

            // 2) Effective OS scheduler quantization for sleeps (cross‑platform)
            // Measure median elapsed for Thread.Sleep(1) across a small sample to infer the scheduler tick.
            static double MeasureEffectiveSleepMs(int iterations)
            {
                // Warmup one sleep to avoid first-iteration anomalies
                Thread.Sleep(1);
                var samples = new double[Math.Max(5, iterations)];
                for (var i = 0; i < samples.Length; i++)
                {
                    var sw = Stopwatch.StartNew();
                    Thread.Sleep(1);
                    sw.Stop();
                    samples[i] = sw.Elapsed.TotalMilliseconds;
                }
                Array.Sort(samples);
                return samples[samples.Length / 2]; // median
            }

            double sleepMedianMs;
            try
            {
                sleepMedianMs = MeasureEffectiveSleepMs(15);
            }
            catch
            {
                sleepMedianMs = double.NaN; // best effort only
            }

            // The gate (and the user's mental model) should reflect what the timer can ACTUALLY
            // resolve, not what it advertises. Fall back to the reported value if the probe failed.
            var gateResolutionNs = haveEffective ? effectiveResolutionNs : reportedResolutionNs;

            var timerKind = isHighRes ? "High-resolution timer" : "Low-resolution timer";
            var resolutionText = haveEffective
                ? $"reported ~{reportedResolutionNs:F0} ns, effective ~{effectiveResolutionNs:F0} ns"
                : $"reported ~{reportedResolutionNs:F0} ns";
            var timerDetails = isHighRes
                ? $"{timerKind}: {resolutionText}"
                : $"{timerKind}: {resolutionText} (low resolution)";

            var sleepDetails = double.IsNaN(sleepMedianMs)
                ? "Sleep(1) median: n/a"
                : $"Sleep(1) median ≈ {sleepMedianMs:F1} ms";

            var details = $"{timerDetails}; {sleepDetails}";

            // PASS when the timer can actually resolve sub-µs work (~<=0.2µs of EFFECTIVE granularity).
            if (isHighRes && gateResolutionNs <= 200)
            {
                return new("Timer", HealthStatus.Pass, details);
            }

            // When the effective granularity is far coarser than advertised, the actionable fix is to
            // raise the work per measurement rather than chase a finer timer.
            var recommendation = haveEffective && effectiveResolutionNs > reportedResolutionNs * 4
                ? "Effective timer granularity is much coarser than advertised; for sub-µs operations raise the work per measurement (OperationsPerInvoke / batch the call) so each sample sits well above the timer floor"
                : "Ensure high-resolution timers; sub-tick sleeps will quantize to the OS scheduler tick";
            return new("Timer", HealthStatus.Warn, details, recommendation);
        }
        catch (Exception ex)
        {
            return new("Timer", HealthStatus.Unknown, ex.Message);
        }
    }

    /// <summary>
    /// Probes the smallest non-zero <see cref="Stopwatch.GetTimestamp"/> delta to estimate the timer's
    /// TRUE granularity, which <see cref="Stopwatch.Frequency"/> does not reveal (it only states the tick
    /// unit). Tight-loops sampling deltas, ignoring zeros (counter hasn't advanced yet), until enough
    /// non-zero deltas are gathered or the iteration cap is hit.
    /// </summary>
    private static double MeasureEffectiveResolutionNs(long freq)
    {
        const int targetNonZeroSamples = 200;
        const int maxIterations = 100_000;
        var deltas = new List<long>(targetNonZeroSamples);
        var previous = Stopwatch.GetTimestamp(); // first reading is discarded (no delta recorded for it)
        var nonZero = 0;
        for (var i = 0; i < maxIterations && nonZero < targetNonZeroSamples; i++)
        {
            var now = Stopwatch.GetTimestamp();
            var delta = now - previous;
            previous = now;
            if (delta <= 0) continue; // 0 = sub-tick (counter unchanged); <0 guards against any wrap
            deltas.Add(delta);
            nonZero++;
        }

        return EffectiveResolutionNsFromTickDeltas(deltas, freq);
    }

    /// <summary>
    /// Pure, deterministic core of the effective-resolution probe (exposed for unit tests). The
    /// smallest non-zero tick delta is the observed quantum; converting via 1e9/freq yields nanoseconds.
    /// Returns <see cref="double.NaN"/> when no advance was observed so callers can fall back to the
    /// reported resolution.
    /// </summary>
    internal static double EffectiveResolutionNsFromTickDeltas(IReadOnlyList<long> deltaTicks, long stopwatchFrequency)
    {
        if (deltaTicks is null || deltaTicks.Count == 0) return double.NaN;
        var nsPerTick = 1_000_000_000.0 / Math.Max(1, stopwatchFrequency);
        var minNonZero = long.MaxValue;
        foreach (var delta in deltaTicks)
        {
            if (delta > 0 && delta < minNonZero) minNonZero = delta;
        }

        return minNonZero == long.MaxValue ? double.NaN : minNonZero * nsPerTick;
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
            var output = proc.StandardOutput.ReadToEnd();
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

    private static async Task<HealthCheckEntry> CheckBackgroundCpuLoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Best-effort, dependency-free approximation: measure current process CPU over a short interval
            var p = Process.GetCurrentProcess();
            var startCpu = p.TotalProcessorTime;
            var sw = Stopwatch.StartNew();
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
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
            return new("Background CPU", status, $"{cpuPct:F0}%", rec);
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

    private static HealthCheckEntry? CheckTimerJitterFromCalibration(ITimerCalibrationResultProvider? provider)
    {
        try
        {
            var r = provider?.Current;
            if (r is null) return null;

            // Thresholds based on RSD%
            var rsd = r.RsdPercent;
            var status = rsd <= 5.0 ? HealthStatus.Pass : rsd <= 15.0 ? HealthStatus.Warn : HealthStatus.Fail;
            var details = $"RSD={rsd:F1}% | Median={r.MedianTicks} ticks | N={r.Samples} (warmup {r.Warmups}) | Score={r.JitterScore}/100";
            var rec = status switch
            {
                HealthStatus.Pass => null,
                HealthStatus.Warn => "Reduce background noise: close apps, pin to 1 core, use High Performance power plan",
                HealthStatus.Fail => "Environment jitter is high. Close background tasks, disable power saving, pin CPU affinity, and retry",
                _ => throw new InvalidOperationException("Encountered unexpected status")
            };
            return new("Timer Jitter", status, details, rec);
        }
        catch (Exception ex)
        {
            return new HealthCheckEntry("Timer Jitter", HealthStatus.Unknown, ex.Message);
        }
    }

}

