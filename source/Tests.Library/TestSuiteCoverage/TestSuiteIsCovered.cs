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
        await new DemoPerformanceTest().DoThing(new CancellationToken());
        new ResolveTestCaseIdTestMultipleCtorArgs(new ExampleDependencyForAltRego(),
            new TestCaseId($"{nameof(ResolveTestCaseIdTestMultipleCtorArgs)}.{nameof(ResolveTestCaseIdTestMultipleCtorArgs.MainMethod)}()")).MainMethod();
        new ScenariosExample().TestMethod(new CancellationToken());
        new MinimalTest().Minimal();
        await new IterationSetupExceptionComesFirst().LifeCycleExceptionTests(new CancellationToken());
        await new IterationSetupExceptionIsHandled().LifeCycleExceptionTests(new CancellationToken());
        await new MethodSetupExceptionIsHandled().LifeCycleExceptionTests(new CancellationToken());
        await new MultipleInjectionsOnAsyncMethod().MainMethod(Substitute.For<ILogger>(), new CancellationToken());
        await new OnlyTheSailfishMethodThrows().MethodTeardown(new CancellationToken());
        await new GlobalSetupExceptionIsHandled().LifeCycleExceptionTests(new CancellationToken());

        new VoidMethodRequestsCancellationToken().MainMethod(new CancellationToken());

        await Should.ThrowAsync<Exception>(async () => await new IterationSetupExceptionComesFirst().MethodTeardown(new CancellationToken()));
        await Should.ThrowAsync<Exception>(async () => await new MethodTeardownExceptionComesFirst().GlobalTeardown(new CancellationToken()));
        await Should.ThrowAsync<Exception>(async () => await new IterationSetupExceptionComesFirst().SailfishMethodException(new CancellationToken()));
    }
}