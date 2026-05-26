using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Sailfish.Execution;

/// <summary>
///     Tracks which (test type, lifecycle method) pairs have already been invoked under the
///     RunOnce contract, so a single lifecycle method that applies to multiple SailfishMethods
///     fires at most once per executor run.
/// </summary>
internal static class LifecycleMethodTracker
{
    private static readonly ConcurrentDictionary<(Type, MethodInfo), byte> Claimed = new();

    /// <summary>
    ///     Atomically claims the pair. Returns <c>true</c> on the first claim (caller should
    ///     invoke the lifecycle method); <c>false</c> on subsequent calls (caller should skip).
    /// </summary>
    public static bool TryClaim(Type testType, MethodInfo lifecycleMethod) =>
        Claimed.TryAdd((testType, lifecycleMethod), 0);

    /// <summary>
    ///     Clears all claims. Called at the start of each executor run so RunOnce state does not
    ///     leak across invocations of <see cref="SailFishTestExecutor.Execute" />.
    /// </summary>
    public static void Reset() => Claimed.Clear();
}
