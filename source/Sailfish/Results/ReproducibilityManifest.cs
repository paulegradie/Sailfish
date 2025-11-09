using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Sailfish.Contracts.Public.Models;
using Sailfish.Diagnostics.Environment;
using Sailfish.Execution;

namespace Sailfish.Results
{
    public sealed class ReproducibilityManifest
    {
        public string SailfishVersion { get; init; } = string.Empty;
        public string? CommitSha { get; init; }
        public string DotNetRuntime { get; init; } = string.Empty;
        public string OS { get; init; } = string.Empty;
        public string OSArchitecture { get; init; } = string.Empty;
        public string ProcessArchitecture { get; init; } = string.Empty;
        public string? CpuModel { get; init; }
        public string GCMode { get; init; } = string.Empty;
        public string Jit { get; init; } = string.Empty;
        public string ProcessPriority { get; init; } = string.Empty;
        public string CpuAffinity { get; init; } = string.Empty;
        public string Timer { get; init; } = string.Empty;
        public int EnvironmentHealthScore { get; init; }
        public string? EnvironmentHealthLabel { get; init; }
        public DateTime TimestampUtc { get; init; }
        public string SessionId { get; init; } = string.Empty;
        public Dictionary<string, string> Tags { get; init; } = new();
        public string? CiSystem { get; init; }

        // Timer calibration snapshot (captured once per session)
        public TimerCalibrationSnapshot? TimerCalibration { get; set; }

        // Per-method snapshot (captured at session end)
        public List<MethodSnapshot> Methods { get; init; } = new();
        public RandomizationConfig Randomization { get; set; } = new();


        public sealed class MethodSnapshot
        {
            public string TestCaseDisplayName { get; init; } = string.Empty;
            public int SampleSize { get; init; }
            public int NumWarmupIterations { get; init; }
            public double Mean { get; init; }
            public double StdDev { get; init; }
            public double? CI95_MarginOfError { get; init; }
            public double? CI99_MarginOfError { get; init; }
        }

        public sealed class TimerCalibrationSnapshot
        {
            public long StopwatchFrequency { get; init; }
            public double ResolutionNs { get; init; }
            public int BaselineOverheadTicks { get; init; }
            public int Warmups { get; init; }
            public int Samples { get; init; }
            public double StdDevTicks { get; init; }
            public long MedianTicks { get; init; }
            public double RsdPercent { get; init; }
            public int JitterScore { get; init; }

            public static TimerCalibrationSnapshot From(TimerCalibrationResult r) => new()
            {
                StopwatchFrequency = r.StopwatchFrequency,
                ResolutionNs = r.ResolutionNs,
                BaselineOverheadTicks = r.BaselineOverheadTicks,
                Warmups = r.Warmups,
                Samples = r.Samples,
                StdDevTicks = r.StdDevTicks,
                MedianTicks = r.MedianTicks,
                RsdPercent = r.RsdPercent,
                JitterScore = r.JitterScore
            };
        }

        public sealed class RandomizationConfig
        {
            public int? Seed { get; init; }
            public bool Types { get; init; }
            public bool Methods { get; init; }
            public bool PropertySets { get; init; }
        }

        public static ReproducibilityManifest CreateBase(IRunSettings runSettings, EnvironmentHealthReport? health)
        {
            var manifest = new ReproducibilityManifest
            {
                SailfishVersion = GetInformationalVersion(typeof(ReproducibilityManifest).Assembly),
                CommitSha = DetectCommitSha(),
                DotNetRuntime = RuntimeInformation.FrameworkDescription,
                OS = RuntimeInformation.OSDescription,
                OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                CpuModel = TryGetCpuModel(),
                GCMode = System.Runtime.GCSettings.IsServerGC ? "Server" : "Workstation",
                Jit = ReadJitFlags(),
                ProcessPriority = TryGetProcessPriority(),
                CpuAffinity = TryGetCpuAffinity(),
                Timer = DescribeTimer(),
                EnvironmentHealthScore = health?.Score ?? 0,
                EnvironmentHealthLabel = health?.SummaryLabel,
                TimestampUtc = DateTime.UtcNow,
                SessionId = BuildSessionId(runSettings),
                Tags = runSettings.Tags.ToDictionary(kv => kv.Key!, kv => kv.Value!)!,
                CiSystem = DetectCiSystem()
            };

            var seed = TryParseSeed(runSettings.Args);
            manifest.Randomization = new RandomizationConfig
            {
                Seed = seed,
                Types = seed.HasValue,
                Methods = seed.HasValue,
                PropertySets = seed.HasValue
            };

            return manifest;
        }

        public void AddMethodSnapshots(IEnumerable<Sailfish.Contracts.Public.Serialization.Tracking.V1.ClassExecutionSummaryTrackingFormat> classes)
        {
            foreach (var cls in classes)
            {
                foreach (var m in cls.CompiledTestCaseResults)
                {
                    var pr = m.PerformanceRunResult;
                    if (pr is null) continue;

                    var n = Math.Max(pr.DataWithOutliersRemoved?.Length ?? 0, 0);
                    double? moe95 = null;
                    double? moe99 = null;
                    if (n > 1 && pr.StdDev > 0)
                    {
                        var se = pr.StdDev / Math.Sqrt(n);
                        var dof = Math.Max(1, n - 1);
                        // Student's t critical values via MathNet
                        try
                        {
                            var t95 = MathNet.Numerics.Distributions.StudentT.InvCDF(0, 1, dof, 0.975);
                            moe95 = t95 * se;
                            var t99 = MathNet.Numerics.Distributions.StudentT.InvCDF(0, 1, dof, 0.995);
                            moe99 = t99 * se;
                        }
                        catch { /* best-effort */ }
                    }

                    Methods.Add(new MethodSnapshot
                    {
                        TestCaseDisplayName = m.TestCaseId?.DisplayName ?? "Unknown",
                        SampleSize = n,
                        NumWarmupIterations = pr.NumWarmupIterations,
                        Mean = pr.Mean,
                        StdDev = pr.StdDev,
                        CI95_MarginOfError = moe95,
                        CI99_MarginOfError = moe99
                    });
                }
            }
        }

        public static void WriteJson(ReproducibilityManifest manifest, string outputDirectory, string? fileName = null)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory)) outputDirectory = Sailfish.Presentation.DefaultFileSettings.DefaultOutputDirectory;
            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);
            fileName ??= $"Manifest_{manifest.TimestampUtc.ToString(Sailfish.Presentation.DefaultFileSettings.SortableFormat)}.json";
            var path = Path.Combine(outputDirectory, fileName);
            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private static string GetInformationalVersion(Assembly asm)
        {
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            return info ?? asm.GetName().Version?.ToString() ?? "unknown";
        }

        private static string DetectCommitSha()
        {
            // Common CI environment variables
            var envs = new[] { "GITHUB_SHA", "BUILD_SOURCEVERSION", "CI_COMMIT_SHA", "SOURCE_VERSION", "VERCEL_GIT_COMMIT_SHA" };
            foreach (var e in envs)
            {
                var v = Environment.GetEnvironmentVariable(e);
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            return null!;
        }

        private static string BuildSessionId(IRunSettings runSettings)
        {
            var ts = runSettings.TimeStamp.ToUniversalTime().ToString(Sailfish.Presentation.DefaultFileSettings.SortableFormat);
            return $"{ts}-{Guid.NewGuid().ToString("N")[..8]}";
        }

        private static string TryGetProcessPriority()
        {
            try { return Process.GetCurrentProcess().PriorityClass.ToString(); } catch { return "Unknown"; }
        }

        private static string TryGetCpuAffinity()
        {
            try
            {
                var p = Process.GetCurrentProcess();
                var mask = (ulong)p.ProcessorAffinity;
                var cores = CountBits(mask);
                return $"Mask=0x{mask:X}; Cores={cores}";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static int CountBits(ulong v)
        {
            var c = 0; while (v != 0) { v &= v - 1; c++; } return c;
        }

        private static string DescribeTimer()
        {
            try
            {
                var freq = Stopwatch.Frequency; var isHigh = Stopwatch.IsHighResolution; var resNs = 1_000_000_000.0 / freq;
                return isHigh ? $"High-resolution (~{resNs:F0} ns)" : $"Low-resolution (~{resNs:F0} ns)";
            }
            catch { return "Unknown"; }
        }

        private static string ReadJitFlags()
        {
            static string ReadFlag(string name)
            {
                var v = Environment.GetEnvironmentVariable(name);
                return string.IsNullOrWhiteSpace(v) ? "default" : v.Trim();
            }
            try
            {
                var tiered = ReadFlag("COMPlus_TieredCompilation");
                var quickJit = ReadFlag("COMPlus_TC_QuickJit");
                var quickJitLoops = ReadFlag("COMPlus_TC_QuickJitForLoops");
                var osr = ReadFlag("COMPlus_TC_OnStackReplacement");
                return $"Tiered={tiered}; QuickJit={quickJit}; QuickJitForLoops={quickJitLoops}; OSR={osr}";
            }
            catch { return "Unknown"; }
        }

        private static int? TryParseSeed(Sailfish.Extensions.Types.OrderedDictionary args)
        {
            try
            {
                foreach (var kv in args)
                {
                    var key = kv.Key;
                    var value = kv.Value;
                    if (string.Equals(key, "seed", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(key, "randomseed", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(key, "rng", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(value, out var s)) return s;
                    }
                }
            }
            catch { /* ignore */ }
            return null;
        }

        private static string? DetectCiSystem()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"))) return "GitHub Actions";
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD"))) return "Azure Pipelines";
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))) return "CI";
            return null;
        }

        private static string? TryGetCpuModel()
        {
            try
            {
#if NET8_0_OR_GREATER
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Lightweight WMI-less approach: read environment if provided by CI, else unknown.
                    var cpuName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
                    if (!string.IsNullOrWhiteSpace(cpuName)) return cpuName;
                }
#endif
            }
            catch { }
            return null;
        }
    }

    public interface IReproducibilityManifestProvider
    {
        ReproducibilityManifest? Current { get; set; }
    }

    internal sealed class ReproducibilityManifestProvider : IReproducibilityManifestProvider
    {
        public ReproducibilityManifest? Current { get; set; }
    }
}

