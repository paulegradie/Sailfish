using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Logging;
using Shouldly;
using Tests.E2E.ExceptionHandling.Tests;
using Tests.E2E.TestSuite.Discoverable;
using Tests.E2E.TestSuite.Utils;
using Xunit;

namespace Tests.Library.TestSuiteCoverage;

public class TestSuiteIsCovered
{
    [Fact]
    public async Task Cover()
    {
        await new DemoPerformanceTest().DoThing(new CancellationToken());
        new ResolveTestCaseIdTestMultipleCtorArgs(
                new ExampleDependencyForAltRego(),
                new TestCaseId($"{nameof(ResolveTestCaseIdTestMultipleCtorArgs)}.{nameof(ResolveTestCaseIdTestMultipleCtorArgs.MainMethod)}()"))
            .MainMethod();

        var se = new ScenariosExample();
        se.GlobalSetup();
        se.Scenario = "ScenarioA";
        se.Scenario = "ScenarioB";
        await se.TestMethod(new CancellationToken());

        new MinimalTest().Minimal();
        new E2E.TestSuite.Discoverable.InnerNamespace.MinimalTest().Minimal();
        var scenarios = new ScenariosExample();
        scenarios.GlobalSetup();
        scenarios.Scenario = "ScenarioA";
        await scenarios.TestMethod(CancellationToken.None);
        await new IterationSetupExceptionComesFirst().LifeCycleExceptionTests(CancellationToken.None);
        await new IterationSetupExceptionIsHandled().LifeCycleExceptionTests(CancellationToken.None);
        await new MethodSetupExceptionIsHandled().LifeCycleExceptionTests(CancellationToken.None);
        await new MultipleInjectionsOnAsyncMethod().MainMethod(Substitute.For<ILogger>(), CancellationToken.None);
        await new OnlyTheSailfishMethodThrows().MethodTeardown(CancellationToken.None);
        await new GlobalSetupExceptionIsHandled().LifeCycleExceptionTests(CancellationToken.None);

        new VoidMethodRequestsCancellationToken().MainMethod(CancellationToken.None);

        await Should.ThrowAsync<Exception>(async () => await new IterationSetupExceptionComesFirst().MethodTeardown(CancellationToken.None));
        await Should.ThrowAsync<Exception>(async () => await new MethodTeardownExceptionComesFirst().GlobalTeardown(CancellationToken.None));
        await Should.ThrowAsync<Exception>(async () => await new IterationSetupExceptionComesFirst().SailfishMethodException(CancellationToken.None));
    }
}