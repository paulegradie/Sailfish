using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Sailfish.Logging;

namespace Sailfish.Execution;

/// <summary>
///     Warns when a discovered test assembly was built without optimizations (a Debug build). Benchmark
///     numbers from unoptimized IL are not representative, and it is the most common real-world cause of
///     meaningless results. The TestAdapter surfaces this via its environment health report; the
///     library/CLI run path did not, so this restores parity by logging a prominent warning once per
///     offending assembly at the start of a run.
/// </summary>
internal static class BuildOptimizationGuard
{
    public static void WarnIfUnoptimized(IEnumerable<Type> testTypes, ILogger logger)
    {
        if (testTypes is null) return;

        var seen = new HashSet<Assembly>();
        foreach (var type in testTypes)
        {
            if (type is null) continue;
            var assembly = type.Assembly;
            if (!seen.Add(assembly)) continue;
            if (!IsUnoptimized(assembly)) continue;

            logger.Log(LogLevel.Warning,
                "Test assembly '{Assembly}' appears to be a Debug (unoptimized) build. Benchmark numbers from an unoptimized build are not representative — build and run against Release/optimized output for meaningful measurements.",
                assembly.GetName().Name ?? assembly.FullName ?? "unknown");
        }
    }

    internal static bool IsUnoptimized(Assembly assembly)
    {
        try
        {
            var dbg = assembly.GetCustomAttribute<DebuggableAttribute>();
            return dbg != null && (dbg.DebuggingFlags & DebuggableAttribute.DebuggingModes.DisableOptimizations) != 0;
        }
        catch
        {
            // Best-effort: if we can't read the attribute, don't block or mislead.
            return false;
        }
    }
}
