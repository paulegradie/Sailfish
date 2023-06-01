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

internal class AncillaryInvocation
{
    private readonly object instance;
    private readonly MethodInfo? globalSetup;
    private readonly MethodInfo? globalTeardown;
    private readonly List<MethodInfo> iterationSetup;
    private readonly List<MethodInfo> iterationTeardown;
    private readonly List<MethodInfo> methodSetup;
    private readonly List<MethodInfo> methodTeardown;
    private readonly PerformanceTimer performanceTimer;

    private readonly MethodInfo mainMethod;

    public AncillaryInvocation(object instance, MethodInfo method, PerformanceTimer performanceTimer)
    {
        this.instance = instance;
        this.performanceTimer = performanceTimer;

        mainMethod = method;
        globalSetup = instance.GetMethodWithAttribute<SailfishGlobalSetupAttribute>();
        globalTeardown = instance.GetMethodWithAttribute<SailfishGlobalTeardownAttribute>();
        methodSetup = instance.GetMethodsWithAttribute<SailfishMethodSetupAttribute>();
        methodTeardown = instance.GetMethodsWithAttribute<SailfishMethodTeardownAttribute>();
        iterationSetup = instance.GetMethodsWithAttribute<SailfishIterationSetupAttribute>();
        iterationTeardown = instance.GetMethodsWithAttribute<SailfishIterationTeardownAttribute>();
    }

    private string MainMethodName => mainMethod.Name;

    /// <summary>
    /// If the setup method doesn't have any method names, then its applied to all methods. 
    /// if it has names, its only applied to those names given
    /// </summary>
    /// <param name="lifecycleMethods"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="SailfishException"></exception>
    private async Task InvokeLifecycleMethods<TAttribute>(IEnumerable<MethodInfo> lifecycleMethods, CancellationToken cancellationToken)
        where TAttribute : Attribute, IInnerLifecycleAttribute
    {
        foreach (var lifecycleMethod in lifecycleMethods.OrderBy(x => x.Name))
        {
            var attribute = lifecycleMethod.GetCustomAttribute<TAttribute>();
            if (attribute is null) throw new SailfishException($"{nameof(TAttribute)}, was somehow missing");
            if (attribute.MethodNames.Length == 0 || attribute.MethodNames.Contains(MainMethodName))
            {
                await lifecycleMethod.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task GlobalSetup(CancellationToken cancellationToken)
    {
        performanceTimer.StartGlobalLifecycleTimer();
        if (globalSetup is not null) await globalSetup.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
    }

    public async Task MethodSetup(CancellationToken cancellationToken)
    {
        performanceTimer.StartMethodLifecycleTimer();
        await InvokeLifecycleMethods<SailfishMethodSetupAttribute>(methodSetup, cancellationToken).ConfigureAwait(false);
    }

    public async Task IterationSetup(CancellationToken cancellationToken)
    {
        await InvokeLifecycleMethods<SailfishIterationSetupAttribute>(iterationSetup, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecutionMethod(CancellationToken cancellationToken, bool timed = true)
    {
        // TODO: we can make this more precise by timing the outside, and then returning
        // a timespan from the wasted bits inside the try invoke
        // add method to timer to .StopExecutionTimerWithAdjustment(timespan);
        // TODO: Action this when we switch over to using ticks instead of ms
        if (timed) performanceTimer.StartSailfishMethodExecutionTimer();
        await mainMethod.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
        if (timed) performanceTimer.StopSailfishMethodExecutionTimer();
    }

    public async Task IterationTearDown(CancellationToken cancellationToken)
    {
        await InvokeLifecycleMethods<SailfishIterationTeardownAttribute>(iterationTeardown, cancellationToken).ConfigureAwait(false);
    }

    public async Task MethodTearDown(CancellationToken cancellationToken)
    {
        await InvokeLifecycleMethods<SailfishMethodTeardownAttribute>(methodTeardown, cancellationToken);
        performanceTimer.StopMethodLifecycleTimer();
    }

    public async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        if (globalTeardown is not null) await globalTeardown.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
        performanceTimer.StopGlobalLifecycleTimer();
    }

    public PerformanceTimer GetPerformanceResults(bool isValid = true)
    {
        if (!isValid)
        {
            performanceTimer.SetAsInvalid();
        }

        return performanceTimer;
    }
}