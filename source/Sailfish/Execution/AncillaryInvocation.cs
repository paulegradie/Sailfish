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
        mainMethod = method;
        this.performanceTimer = performanceTimer;
        globalSetup = instance.GetMethodWithAttribute<SailfishGlobalSetupAttribute>();
        globalTeardown = instance.GetMethodWithAttribute<SailfishGlobalTeardownAttribute>();
        methodSetup = instance.GetMethodWithAttribute<SailfishMethodSetupAttribute>();
        methodTeardown = instance.GetMethodWithAttribute<SailfishMethodTeardownAttribute>();
        iterationSetup = instance.GetMethodWithAttribute<SailfishIterationSetupAttribute>();
        iterationTeardown = instance.GetMethodWithAttribute<SailfishIterationTeardownAttribute>();
    }

    public async Task ExecutionMethod(CancellationToken cancellationToken, bool timed = true)
    {
        if (timed) performanceTimer.StartExecutionTimer();
        await mainMethod.InvokeWith(instance, cancellationToken).ConfigureAwait(false);
        if (timed) performanceTimer.StopExecutionTimer();
    }

    public async Task IterationSetup(CancellationToken cancellationToken)
    {
        if (iterationSetup is not null) await iterationSetup.InvokeWith(instance, cancellationToken).ConfigureAwait(false);
    }

    public async Task IterationTearDown(CancellationToken cancellationToken)
    {
        if (iterationTeardown is not null) await iterationTeardown.InvokeWith(instance, cancellationToken).ConfigureAwait(false);
    }

    public async Task MethodSetup(CancellationToken cancellationToken)
    {
        performanceTimer.StartMethodTimer();
        if (methodSetup is not null) await methodSetup.InvokeWith(instance, cancellationToken).ConfigureAwait(false);
    }

    public async Task MethodTearDown(CancellationToken cancellationToken)
    {
        if (methodTeardown is not null) await methodTeardown.InvokeWith(instance, cancellationToken).ConfigureAwait(false);
        performanceTimer.StopMethodTimer();
    }


    public async Task GlobalSetup(CancellationToken cancellationToken)
    {
        performanceTimer.StartGlobalTimer();
        if (globalSetup is not null) await globalSetup.InvokeWith(instance, cancellationToken).ConfigureAwait(false);
    }

    public async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        if (globalTeardown is not null) await globalTeardown.InvokeWith(instance, cancellationToken).ConfigureAwait(false);
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