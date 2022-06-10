using System.Reflection;
using VeerPerforma.Attributes;
using VeerPerforma.Utils;

namespace VeerPerforma.Execution;

public class AncillaryInvocation
{
    private readonly object instance;
    private readonly MethodInfo? globalSetup;
    private readonly MethodInfo? globalTeardown;
    private readonly MethodInfo mainMethod;
    private readonly MethodInfo? methodSetup;
    private readonly MethodInfo? methodTeardown;
    private readonly MethodInfo? iterationSetup;
    private readonly MethodInfo? iterationTeardown;

    public AncillaryInvocation(object instance, MethodInfo method)
    {
        this.instance = instance;
        mainMethod = method;
        globalSetup = instance.GetMethodWithAttribute<VeerGlobalSetupAttribute>();
        globalTeardown = instance.GetMethodWithAttribute<VeerGlobalTeardownAttribute>();
        methodSetup = instance.GetMethodWithAttribute<VeerExecutionMethodSetupAttribute>();
        methodTeardown = instance.GetMethodWithAttribute<VeerExecutionMethodTeardownAttribute>();
        iterationSetup = instance.GetMethodWithAttribute<VeerExecutionIterationSetupAttribute>();
        iterationTeardown = instance.GetMethodWithAttribute<VeerExecutionIterationTeardownAttribute>();
    }

    public async Task ExecutionMethod()
    {
        await mainMethod.InvokeWith(instance);
    }

    public async Task IterationSetup()
    {
        if (iterationSetup is not null) await iterationSetup.InvokeWith(instance);
    }

    public async Task IterationTearDown()
    {
        if (iterationTeardown is not null) await iterationTeardown.InvokeWith(instance);
    }

    public async Task MethodSetup()
    {
        if (methodSetup is not null) await methodSetup.InvokeWith(instance);
    }

    public async Task MethodTearDown()
    {
        if (methodTeardown is not null) await methodTeardown.InvokeWith(instance);
    }


    public async Task GlobalSetup()
    {
        if (globalSetup is not null) await globalSetup.InvokeWith(instance);
    }

    public async Task GlobalTeardown()
    {
        if (globalTeardown is not null) await globalTeardown.InvokeWith(instance);
    }
}