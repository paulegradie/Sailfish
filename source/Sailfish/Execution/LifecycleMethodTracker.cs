using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Sailfish.Execution;

/// <summary>
///     Tracks which (test type, lifecycle method) pairs have already been invoked under the
///     RunOnce contract, so a single lifecycle method that applies to multiple SailfishMethods
///     fires at most once per executor run.
/// </summary>
/// <remarks>
///     Each executor run owns its own instance of this tracker — both
///     <see cref="SailFishTestExecutor" /> and the test-adapter execution engine create a fresh
///     tracker per <c>Execute(...)</c> invocation. This scoping prevents cross-run state leakage
///     (claims from a prior run silently skipping lifecycle methods in a later run within the same
///     testhost process) and avoids interference between concurrent runs.
/// </remarks>
internal sealed class LifecycleMethodTracker
{
    private readonly ConcurrentDictionary<(Type, MethodInfo), byte> _claimed = new();

    /// <summary>
    ///     Atomically claims the pair. Returns <c>true</c> on the first claim (caller should
    ///     invoke the lifecycle method); <c>false</c> on subsequent calls (caller should skip).
    /// </summary>
    public bool TryClaim(Type testType, MethodInfo lifecycleMethod) =>
        _claimed.TryAdd((testType, lifecycleMethod), 0);
}
