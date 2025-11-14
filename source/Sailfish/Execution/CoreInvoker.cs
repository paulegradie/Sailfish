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

    private readonly MethodInfo _mainMethod;
    private readonly List<MethodInfo> _methodSetup;
    private readonly List<MethodInfo> _methodTeardown;
    private readonly PerformanceTimer _testCasePerformanceTimer;

    public CoreInvoker(object instance, MethodInfo method, PerformanceTimer testCasePerformanceTimer)
    {
        _globalSetup = instance.FindMethodsDecoratedWithAttribute<SailfishGlobalSetupAttribute>();
        _globalTeardown = instance.FindMethodsDecoratedWithAttribute<SailfishGlobalTeardownAttribute>();
        this._instance = instance;
        _iterationSetup = instance.FindMethodsDecoratedWithAttribute<SailfishIterationSetupAttribute>();
        _iterationTeardown = instance.FindMethodsDecoratedWithAttribute<SailfishIterationTeardownAttribute>();
        _mainMethod = method;
        _methodSetup = instance.FindMethodsDecoratedWithAttribute<SailfishMethodSetupAttribute>();
        _methodTeardown = instance.FindMethodsDecoratedWithAttribute<SailfishMethodTeardownAttribute>();
        this._testCasePerformanceTimer = testCasePerformanceTimer;
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
            if (attribute.MethodNames.Length == 0 || attribute.MethodNames.Contains(MainMethodName))
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

    public async Task ExecutionMethod(CancellationToken cancellationToken, bool timed = true)
    {
        await _mainMethod.TryInvoke(_instance, cancellationToken, timed ? _testCasePerformanceTimer : null).ConfigureAwait(false);
    }

        public async Task ExecutionMethodWithOperationsPerInvoke(int operationsPerInvoke, CancellationToken cancellationToken)
        {
            if (operationsPerInvoke <= 1)
            {
                await ExecutionMethod(cancellationToken).ConfigureAwait(false);
                return;
            }

            // Aggregate timing over multiple operations within a single measured iteration
            _testCasePerformanceTimer.StartSailfishMethodExecutionTimer();
            try
            {
                for (var i = 0; i < operationsPerInvoke; i++)
                {
                    // Invoke without per-call timing; we capture the aggregate
                    await _mainMethod.TryInvoke(_instance, cancellationToken, null).ConfigureAwait(false);
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