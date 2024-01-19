using Sailfish.Attributes;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal class CoreInvoker(object instance, MethodInfo method, PerformanceTimer testCasePerformanceTimer)
{
    private readonly List<MethodInfo> globalSetup = instance.FindMethodsDecoratedWithAttribute<SailfishGlobalSetupAttribute>();
    private readonly List<MethodInfo> globalTeardown = instance.FindMethodsDecoratedWithAttribute<SailfishGlobalTeardownAttribute>();
    private readonly object instance = instance;
    private readonly List<MethodInfo> iterationSetup = instance.FindMethodsDecoratedWithAttribute<SailfishIterationSetupAttribute>();
    private readonly List<MethodInfo> iterationTeardown = instance.FindMethodsDecoratedWithAttribute<SailfishIterationTeardownAttribute>();

    private readonly MethodInfo mainMethod = method;
    private readonly List<MethodInfo> methodSetup = instance.FindMethodsDecoratedWithAttribute<SailfishMethodSetupAttribute>();
    private readonly List<MethodInfo> methodTeardown = instance.FindMethodsDecoratedWithAttribute<SailfishMethodTeardownAttribute>();
    private readonly PerformanceTimer testCasePerformanceTimer = testCasePerformanceTimer;

    public int OverheadEstimate { get; set; }
    private string MainMethodName => mainMethod.Name;

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
                await lifecycleMethod.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task GlobalSetup(CancellationToken cancellationToken)
    {
        foreach (var lifecycleMethod in globalSetup) await lifecycleMethod.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
    }

    public async Task MethodSetup(CancellationToken cancellationToken)
    {
        await InvokeLifecycleMethods<SailfishMethodSetupAttribute>(methodSetup, cancellationToken).ConfigureAwait(false);
    }

    public async Task IterationSetup(CancellationToken cancellationToken)
    {
        await InvokeLifecycleMethods<SailfishIterationSetupAttribute>(iterationSetup, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecutionMethod(CancellationToken cancellationToken, bool timed = true)
    {
        await mainMethod.TryInvoke(instance, cancellationToken, timed ? testCasePerformanceTimer : null).ConfigureAwait(false);
    }

    public async Task IterationTearDown(CancellationToken cancellationToken)
    {
        await InvokeLifecycleMethods<SailfishIterationTeardownAttribute>(iterationTeardown, cancellationToken).ConfigureAwait(false);
    }

    public async Task MethodTearDown(CancellationToken cancellationToken)
    {
        await InvokeLifecycleMethods<SailfishMethodTeardownAttribute>(methodTeardown, cancellationToken);
    }

    public async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        foreach (var lifecycleMethod in globalTeardown) await lifecycleMethod.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
    }

    public PerformanceTimer GetPerformanceResults(bool isValid = true)
    {
        if (!isValid) testCasePerformanceTimer.SetAsInvalid();

        if (OverheadEstimate > 0) testCasePerformanceTimer.ApplyOverheadEstimate(OverheadEstimate);

        return testCasePerformanceTimer;
    }

    public void AssignOverheadEstimate(int overheadEstimate)
    {
        OverheadEstimate = overheadEstimate;
    }

    public void SetTestCaseStart()
    {
        testCasePerformanceTimer.SetTestCaseStart();
    }

    public void SetTestCaseStop()
    {
        testCasePerformanceTimer.SetTestCaseStop();
    }
}