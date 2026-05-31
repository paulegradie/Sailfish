using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

internal class CoreInvoker
{
    private readonly List<MethodInfo> _globalSetup;
    private readonly List<MethodInfo> _globalTeardown;
    private readonly object _instance;
    private readonly List<MethodInfo> _iterationSetup;
    private readonly List<MethodInfo> _iterationTeardown;
    private readonly LifecycleMethodTracker _lifecycleMethodTracker;

    private readonly MethodInfo _mainMethod;
    private readonly List<MethodInfo> _methodSetup;
    private readonly List<MethodInfo> _methodTeardown;
    private readonly PerformanceTimer _testCasePerformanceTimer;

    // Compiled, allocation-free direct-call invoker for the timed method. Built lazily on first
    // invocation so a malformed signature still surfaces at the same point reflection would have thrown.
    private Func<CancellationToken, ValueTask>? _compiledInvoke;

    public CoreInvoker(object instance, MethodInfo method, PerformanceTimer testCasePerformanceTimer, LifecycleMethodTracker? lifecycleMethodTracker = null)
    {
        _globalSetup = instance.FindMethodsDecoratedWithAttribute<SailfishGlobalSetupAttribute>();
        _globalTeardown = instance.FindMethodsDecoratedWithAttribute<SailfishGlobalTeardownAttribute>();
        _instance = instance;
        _iterationSetup = instance.FindMethodsDecoratedWithAttribute<SailfishIterationSetupAttribute>();
        _iterationTeardown = instance.FindMethodsDecoratedWithAttribute<SailfishIterationTeardownAttribute>();
        _lifecycleMethodTracker = lifecycleMethodTracker ?? new LifecycleMethodTracker();
        _mainMethod = method;
        _methodSetup = instance.FindMethodsDecoratedWithAttribute<SailfishMethodSetupAttribute>();
        _methodTeardown = instance.FindMethodsDecoratedWithAttribute<SailfishMethodTeardownAttribute>();
        _testCasePerformanceTimer = testCasePerformanceTimer;
    }

    public int OverheadEstimate { get; set; }
    private string MainMethodName => _mainMethod.Name;

    /// <summary>
    ///     If the setup method doesn't have any method names, then its applied to all methods.
    ///     if it has names, its only applied to those names given
    /// </summary>
    /// <param name="lifecycleMethods"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="SailfishException"></exception>
    private async Task InvokeLifecycleMethods<TAttribute>(IEnumerable<MethodInfo> lifecycleMethods, CancellationToken cancellationToken)
        where TAttribute : Attribute, IInnerLifecycleAttribute
    {
        foreach (var lifecycleMethod in lifecycleMethods)
        {
            var attribute = lifecycleMethod.GetCustomAttribute<TAttribute>();
            if (attribute is null) throw new SailfishException($"{nameof(TAttribute)}, was somehow missing");
            if (attribute.MethodNames.Length > 0 && !attribute.MethodNames.Contains(MainMethodName)) continue;
            if (attribute.RunOnce && !_lifecycleMethodTracker.TryClaim(_instance.GetType(), lifecycleMethod)) continue;
            await lifecycleMethod.TryInvoke(_instance, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task GlobalSetup(CancellationToken cancellationToken)
    {
        foreach (var lifecycleMethod in _globalSetup) await lifecycleMethod.TryInvoke(_instance, cancellationToken).ConfigureAwait(false);
    }

    public async Task MethodSetup(CancellationToken cancellationToken)
    {
        await InvokeLifecycleMethods<SailfishMethodSetupAttribute>(_methodSetup, cancellationToken).ConfigureAwait(false);
    }

    public async Task IterationSetup(CancellationToken cancellationToken)
    {
        await InvokeLifecycleMethods<SailfishIterationSetupAttribute>(_iterationSetup, cancellationToken).ConfigureAwait(false);
    }

    // The compiled invoker calls the method directly (no reflection, no per-call argument array).
    // The await happens inside the timed region so async work is included in the measurement.
    private Func<CancellationToken, ValueTask> CompiledMainMethod => _compiledInvoke ??= CompiledInvoker.Build(_instance, _mainMethod);

    public async Task ExecutionMethod(CancellationToken cancellationToken, bool timed = true)
    {
        var invoke = CompiledMainMethod;
        if (timed) _testCasePerformanceTimer.StartSailfishMethodExecutionTimer();
        try
        {
            await invoke(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (timed) _testCasePerformanceTimer.StopSailfishMethodExecutionTimer();
        }
    }

    public async Task ExecutionMethodWithOperationsPerInvoke(int operationsPerInvoke, CancellationToken cancellationToken)
    {
        if (operationsPerInvoke <= 1)
        {
            await ExecutionMethod(cancellationToken).ConfigureAwait(false);
            return;
        }

        // Aggregate timing over multiple direct invocations within a single measured iteration.
        var invoke = CompiledMainMethod;
        _testCasePerformanceTimer.StartSailfishMethodExecutionTimer();
        try
        {
            for (var i = 0; i < operationsPerInvoke; i++)
            {
                await invoke(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _testCasePerformanceTimer.StopSailfishMethodExecutionTimer();
        }
    }


    public async Task IterationTearDown(CancellationToken cancellationToken)
    {
        await InvokeLifecycleMethods<SailfishIterationTeardownAttribute>(_iterationTeardown, cancellationToken).ConfigureAwait(false);
    }

    public async Task MethodTearDown(CancellationToken cancellationToken)
    {
        await InvokeLifecycleMethods<SailfishMethodTeardownAttribute>(_methodTeardown, cancellationToken);
    }

    public async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        foreach (var lifecycleMethod in _globalTeardown) await lifecycleMethod.TryInvoke(_instance, cancellationToken).ConfigureAwait(false);
    }

    public PerformanceTimer GetPerformanceResults(bool isValid = true)
    {
        if (!isValid) _testCasePerformanceTimer.SetAsInvalid();

        if (OverheadEstimate > 0) _testCasePerformanceTimer.ApplyOverheadEstimate(OverheadEstimate);

        return _testCasePerformanceTimer;
    }

    public void AssignOverheadEstimate(int overheadEstimate)
    {
        OverheadEstimate = overheadEstimate;
    }

    public void SetTestCaseStart()
    {
        _testCasePerformanceTimer.SetTestCaseStart();
    }

    public void SetTestCaseStop()
    {
        _testCasePerformanceTimer.SetTestCaseStop();
    }


    internal void SetOverheadDisabled(bool disabled)
    {
        _testCasePerformanceTimer.OverheadEstimationDisabled = disabled;
    }

    internal void SetOverheadDiagnostics(int baselineTicks, double driftPercent, int warmups, int samples)
    {
        _testCasePerformanceTimer.OverheadBaselineTicks = baselineTicks;
        _testCasePerformanceTimer.OverheadDriftPercent = driftPercent;
        _testCasePerformanceTimer.OverheadWarmupCount = warmups;
        _testCasePerformanceTimer.OverheadSampleCount = samples;
    }
}