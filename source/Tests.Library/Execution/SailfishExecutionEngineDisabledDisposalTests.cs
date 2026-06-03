using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.Presentation.Console;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

// Regression for #300 (CodeRabbit follow-up): a disabled test is materialized WITHOUT running its constructor
// (TypeActivator allocates it via RuntimeHelpers.GetUninitializedObject), so the instance is uninitialized. The
// engine still routes that instance through DisposeOfTestInstance, which must NOT invoke the user's
// IDisposable/IAsyncDisposable.Dispose — doing so runs user teardown against a half-built object, breaking the
// "no user code for disabled tests" guarantee and potentially reporting a disabled test as failed.
public class SailfishExecutionEngineDisabledDisposalTests
{
    [Sailfish(Disabled = true)]
    private class DisabledDisposableClass : IDisposable, IAsyncDisposable
    {
        public static bool DisposeCalled;
        public static bool DisposeAsyncCalled;

        [SailfishMethod]
        public void Method() { }

        public void Dispose() => DisposeCalled = true;

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCalled = true;
            return ValueTask.CompletedTask;
        }
    }

    [Sailfish] // enabled class ...
    private class EnabledClassWithDisabledDisposableMethod : IDisposable, IAsyncDisposable
    {
        public static bool DisposeCalled;
        public static bool DisposeAsyncCalled;

        [SailfishMethod(Disabled = true)] // ... with a disabled method
        public void Method() { }

        public void Dispose() => DisposeCalled = true;

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCalled = true;
            return ValueTask.CompletedTask;
        }
    }

    private static SailfishExecutionEngine BuildEngine(out IMediator mediator)
    {
        mediator = Substitute.For<IMediator>();
        return new SailfishExecutionEngine(
            Substitute.For<ILogger>(),
            Substitute.For<IConsoleWriter>(),
            Substitute.For<ITestCaseIterator>(),
            Substitute.For<ITestCaseCountPrinter>(),
            mediator,
            Substitute.For<IClassExecutionSummaryCompiler>(),
            Substitute.For<IRunSettings>());
    }

    private static TestInstanceContainerProvider ProviderFor(Type testType)
    {
        var method = testType.GetMethod(nameof(DisabledDisposableClass.Method))!;
        return new TestInstanceContainerProvider(
            Substitute.For<IRunSettings>(),
            new TypeActivator(new ServiceCollection().BuildServiceProvider()),
            testType,
            new List<PropertySet>(),
            method);
    }

    // A disabled class short-circuits at the top of ActivateContainer (line ~138) and disposes the uninitialized
    // instance there.
    [Fact]
    public async Task DisabledClass_ThatIsDisposable_IsNeverDisposed()
    {
        DisabledDisposableClass.DisposeCalled = false;
        DisabledDisposableClass.DisposeAsyncCalled = false;

        var engine = BuildEngine(out var mediator);

        var results = await engine.ActivateContainer(ProviderFor(typeof(DisabledDisposableClass)), CancellationToken.None);

        results.ShouldBeEmpty(); // the disabled short-circuit produces no execution results
        await mediator.Received(1).Publish(Arg.Any<TestCaseDisabledNotification>(), Arg.Any<CancellationToken>());

        // The uninitialized instance's disposal must never run (its constructor never did).
        DisabledDisposableClass.DisposeCalled.ShouldBeFalse();
        DisabledDisposableClass.DisposeAsyncCalled.ShouldBeFalse();
    }

    // A disabled method on an enabled class is skipped inside the per-case loop, but its (uninitialized) instance
    // still reaches DisposeOfTestInstance via the loop's finally (line ~226) — that path must be guarded too.
    [Fact]
    public async Task DisabledMethod_OnEnabledClass_DoesNotDisposeUninitializedInstance()
    {
        EnabledClassWithDisabledDisposableMethod.DisposeCalled = false;
        EnabledClassWithDisabledDisposableMethod.DisposeAsyncCalled = false;

        var engine = BuildEngine(out var mediator);

        await engine.ActivateContainer(ProviderFor(typeof(EnabledClassWithDisabledDisposableMethod)), CancellationToken.None);

        await mediator.Received(1).Publish(Arg.Any<TestCaseDisabledNotification>(), Arg.Any<CancellationToken>());

        EnabledClassWithDisabledDisposableMethod.DisposeCalled.ShouldBeFalse();
        EnabledClassWithDisabledDisposableMethod.DisposeAsyncCalled.ShouldBeFalse();
    }
}
