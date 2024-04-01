using NSubstitute;
using Sailfish.Contracts.Public.Models;
using System.Threading;
using System.Threading.Tasks;
using Tests.E2E.ExceptionHandling.Tests;
using Tests.E2E.TestSuite.Discoverable;
using Tests.E2E.TestSuite.Utils;
using Xunit;
using Sailfish.Logging;
using Shouldly;
using System;

namespace Tests.Library.TestSuiteCoverage;

public class TestSuiteIsCovered
{
    [Fact]
    public async Task Cover()
    {
        await new DemoPerformanceTest().DoThing(CancellationToken.None);
        new ResolveTestCaseIdTestMultipleCtorArgs(
            new ExampleDependencyForAltRego(),
            new TestCaseId($"{nameof(ResolveTestCaseIdTestMultipleCtorArgs)}.{nameof(ResolveTestCaseIdTestMultipleCtorArgs.MainMethod)}()")).MainMethod();
        new MinimalTest().Minimal();
        new Tests.E2E.TestSuite.Discoverable.InnerNamespace.MinimalTest().Minimal();
        var scenarios = new ScenariosExample();
        scenarios.GlobalSetup();
        scenarios.TestMethod(CancellationToken.None);
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