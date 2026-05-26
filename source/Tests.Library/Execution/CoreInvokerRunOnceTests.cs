using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class CoreInvokerRunOnceTests
{
    public CoreInvokerRunOnceTests()
    {
        LifecycleMethodTracker.Reset();
    }

    [Fact]
    public async Task RunOnceMethodSetup_AppliedToTwoMethods_InvokesSetupOnlyOnce()
    {
        var instanceA = new TestClassWithSharedRunOnceSetup();
        var instanceB = new TestClassWithSharedRunOnceSetup();
        var methodA = typeof(TestClassWithSharedRunOnceSetup).GetMethod(nameof(TestClassWithSharedRunOnceSetup.MethodA))!;
        var methodB = typeof(TestClassWithSharedRunOnceSetup).GetMethod(nameof(TestClassWithSharedRunOnceSetup.MethodB))!;

        var invokerA = new CoreInvoker(instanceA, methodA, new PerformanceTimer());
        var invokerB = new CoreInvoker(instanceB, methodB, new PerformanceTimer());

        await invokerA.MethodSetup(CancellationToken.None);
        await invokerB.MethodSetup(CancellationToken.None);

        // Setup was claimed by the first invoker; the second skipped it.
        (instanceA.SetupCalls + instanceB.SetupCalls).ShouldBe(1);
    }

    [Fact]
    public async Task RunOnceFalse_PreservesDefaultBehavior_RunsSetupPerMethod()
    {
        var instanceA = new TestClassWithSharedSetup();
        var instanceB = new TestClassWithSharedSetup();
        var methodA = typeof(TestClassWithSharedSetup).GetMethod(nameof(TestClassWithSharedSetup.MethodA))!;
        var methodB = typeof(TestClassWithSharedSetup).GetMethod(nameof(TestClassWithSharedSetup.MethodB))!;

        var invokerA = new CoreInvoker(instanceA, methodA, new PerformanceTimer());
        var invokerB = new CoreInvoker(instanceB, methodB, new PerformanceTimer());

        await invokerA.MethodSetup(CancellationToken.None);
        await invokerB.MethodSetup(CancellationToken.None);

        instanceA.SetupCalls.ShouldBe(1);
        instanceB.SetupCalls.ShouldBe(1);
    }

    [Fact]
    public async Task RunOnceMethodTeardown_AppliedToTwoMethods_InvokesTeardownOnlyOnce()
    {
        var instanceA = new TestClassWithSharedRunOnceTeardown();
        var instanceB = new TestClassWithSharedRunOnceTeardown();
        var methodA = typeof(TestClassWithSharedRunOnceTeardown).GetMethod(nameof(TestClassWithSharedRunOnceTeardown.MethodA))!;
        var methodB = typeof(TestClassWithSharedRunOnceTeardown).GetMethod(nameof(TestClassWithSharedRunOnceTeardown.MethodB))!;

        var invokerA = new CoreInvoker(instanceA, methodA, new PerformanceTimer());
        var invokerB = new CoreInvoker(instanceB, methodB, new PerformanceTimer());

        await invokerA.MethodTearDown(CancellationToken.None);
        await invokerB.MethodTearDown(CancellationToken.None);

        (instanceA.TeardownCalls + instanceB.TeardownCalls).ShouldBe(1);
    }

    [Fact]
    public async Task RunOnceIterationSetup_FiresOnlyOncePerExecutorRun()
    {
        var instance = new TestClassWithRunOnceIterationSetup();
        var methodA = typeof(TestClassWithRunOnceIterationSetup).GetMethod(nameof(TestClassWithRunOnceIterationSetup.MethodA))!;

        var invoker = new CoreInvoker(instance, methodA, new PerformanceTimer());
        await invoker.IterationSetup(CancellationToken.None);
        await invoker.IterationSetup(CancellationToken.None);
        await invoker.IterationSetup(CancellationToken.None);

        instance.IterationSetupCalls.ShouldBe(1);
    }

    [Fact]
    public async Task ResetBetweenExecutorRuns_LetsRunOnceFireAgainInNewRun()
    {
        var firstRun = new TestClassWithSharedRunOnceSetup();
        var methodA = typeof(TestClassWithSharedRunOnceSetup).GetMethod(nameof(TestClassWithSharedRunOnceSetup.MethodA))!;
        var invoker1 = new CoreInvoker(firstRun, methodA, new PerformanceTimer());
        await invoker1.MethodSetup(CancellationToken.None);
        firstRun.SetupCalls.ShouldBe(1);

        LifecycleMethodTracker.Reset();

        var secondRun = new TestClassWithSharedRunOnceSetup();
        var invoker2 = new CoreInvoker(secondRun, methodA, new PerformanceTimer());
        await invoker2.MethodSetup(CancellationToken.None);
        secondRun.SetupCalls.ShouldBe(1);
    }

    [Sailfish]
    public class TestClassWithSharedRunOnceSetup
    {
        public int SetupCalls { get; private set; }

        [SailfishMethodSetup(nameof(MethodA), nameof(MethodB), RunOnce = true)]
        public void Setup() => SetupCalls++;

        [SailfishMethod]
        public void MethodA() { }

        [SailfishMethod]
        public void MethodB() { }
    }

    [Sailfish]
    public class TestClassWithSharedSetup
    {
        public int SetupCalls { get; private set; }

        [SailfishMethodSetup(nameof(MethodA), nameof(MethodB))]
        public void Setup() => SetupCalls++;

        [SailfishMethod]
        public void MethodA() { }

        [SailfishMethod]
        public void MethodB() { }
    }

    [Sailfish]
    public class TestClassWithSharedRunOnceTeardown
    {
        public int TeardownCalls { get; private set; }

        [SailfishMethodTeardown(nameof(MethodA), nameof(MethodB), RunOnce = true)]
        public void Teardown() => TeardownCalls++;

        [SailfishMethod]
        public void MethodA() { }

        [SailfishMethod]
        public void MethodB() { }
    }

    [Sailfish]
    public class TestClassWithRunOnceIterationSetup
    {
        public int IterationSetupCalls { get; private set; }

        [SailfishIterationSetup(RunOnce = true)]
        public void IterSetup() => IterationSetupCalls++;

        [SailfishMethod]
        public void MethodA() { }
    }
}
