using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.ExtensionMethods;

namespace Sailfish.Execution;

internal class AncillaryInvocation
{
    private readonly MethodInfo? globalSetup;
    private readonly MethodInfo? globalTeardown;
    private readonly object instance;
    private readonly MethodInfo? iterationSetup;
    private readonly MethodInfo? iterationTeardown;
    private readonly MethodInfo? methodSetup;
    private readonly MethodInfo? methodTeardown;
    private readonly PerformanceTimer performanceTimer;

    private readonly MethodInfo mainMethod;

    public AncillaryInvocation(object instance, MethodInfo method, PerformanceTimer performanceTimer)
    {
        this.instance = instance;
        this.performanceTimer = performanceTimer;

        mainMethod = method;
        globalSetup = instance.GetMethodWithAttribute<SailfishGlobalSetupAttribute>();
        globalTeardown = instance.GetMethodWithAttribute<SailfishGlobalTeardownAttribute>();
        methodSetup = instance.GetMethodWithAttribute<SailfishMethodSetupAttribute>();
        methodTeardown = instance.GetMethodWithAttribute<SailfishMethodTeardownAttribute>();
        iterationSetup = instance.GetMethodWithAttribute<SailfishIterationSetupAttribute>();
        iterationTeardown = instance.GetMethodWithAttribute<SailfishIterationTeardownAttribute>();
    }

    public async Task ExecutionMethod(CancellationToken cancellationToken, bool timed = true)
    {
        // TODO: we can make this more precise by timing the outside, and then returning
        // a timespan from the wasted bits inside the try invocate
        // add method to timer to .StopExecutionTimerWithAdjustment(timespan);
        if (timed) performanceTimer.StartExecutionTimer();
        await mainMethod.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
        if (timed) performanceTimer.StopExecutionTimer();
    }

    public async Task IterationSetup(CancellationToken cancellationToken)
    {
        if (iterationSetup is not null) await iterationSetup.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
    }

    public async Task IterationTearDown(CancellationToken cancellationToken)
    {
        if (iterationTeardown is not null) await iterationTeardown.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
    }

    public async Task MethodSetup(CancellationToken cancellationToken)
    {
        performanceTimer.StartMethodTimer();
        if (methodSetup is not null) await methodSetup.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
    }

    public async Task MethodTearDown(CancellationToken cancellationToken)
    {
        if (methodTeardown is not null) await methodTeardown.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
        performanceTimer.StopMethodTimer();
    }

    public async Task GlobalSetup(CancellationToken cancellationToken)
    {
        performanceTimer.StartGlobalTimer();
        if (globalSetup is not null) await globalSetup.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
    }

    public async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        if (globalTeardown is not null) await globalTeardown.TryInvoke(instance, cancellationToken).ConfigureAwait(false);
        performanceTimer.StopGlobalTimer();
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