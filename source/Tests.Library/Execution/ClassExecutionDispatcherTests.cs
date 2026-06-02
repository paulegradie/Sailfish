using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class ClassExecutionDispatcherTests
{
    [Sailfish] // default SharedInstance
    private class SharedProbe
    {
        [SailfishMethod]
        public void M() { }
    }

    // Regression for the review Blocker: in SharedInstance mode a constructor / DI-resolution failure must publish a
    // TestCaseExceptionNotification (the test adapter records pass/fail solely from it). A null container tells the
    // adapter to fail every case in the group rather than letting the failure vanish.
    [Fact]
    public async Task SharedInstance_ConstructorFailure_PublishesWholeClassExceptionNotification()
    {
        var mediator = Substitute.For<IMediator>();
        var engine = Substitute.For<ISailfishExecutionEngine>();
        var typeActivator = Substitute.For<ITypeActivator>();
        typeActivator
            .CreateDehydratedTestInstance(Arg.Any<Type>(), Arg.Any<TestCaseId>(), Arg.Any<bool>())
            .Throws(new InvalidOperationException("ctor boom"));

        var method = typeof(SharedProbe).GetMethod(nameof(SharedProbe.M))!;
        var provider = new TestInstanceContainerProvider(
            Substitute.For<IRunSettings>(), typeActivator, typeof(SharedProbe), new List<PropertySet>(), method);

        var dispatcher = new ClassExecutionDispatcher(engine, typeActivator, mediator);

        var results = await dispatcher.Dispatch(
            typeof(SharedProbe), new[] { provider }, new List<dynamic>(), CancellationToken.None);

        results.Count.ShouldBe(1);
        results[0].IsSuccess.ShouldBeFalse();

        await mediator.Received(1).Publish(
            Arg.Is<TestCaseExceptionNotification>(n => n.TestInstanceContainer == null && n.Exception != null),
            Arg.Any<CancellationToken>());

        // The engine must not run anything once construction failed.
        await engine.DidNotReceiveWithAnyArgs().ActivateContainer(default!, default, default!, default);
    }

    // If materializing the representative case throws BEFORE GlobalSetup (e.g. variable hydration fails), the
    // dispatcher must still publish a whole-class exception notification rather than letting it escape unreported.
    [Fact]
    public async Task SharedInstance_RepresentativeCaseMaterializationFailure_PublishesWholeClassExceptionNotification()
    {
        var mediator = Substitute.For<IMediator>();
        var engine = Substitute.For<ISailfishExecutionEngine>();
        var typeActivator = Substitute.For<ITypeActivator>();
        typeActivator
            .CreateDehydratedTestInstance(Arg.Any<Type>(), Arg.Any<TestCaseId>(), Arg.Any<bool>())
            .Returns(_ => new TestInstanceActivation(new SharedProbe(), null));

        var method = typeof(SharedProbe).GetMethod(nameof(SharedProbe.M))!;
        // A variable set naming a property that doesn't exist makes hydration throw while the first case is being
        // materialized (before GlobalSetup).
        var bogusPropertySet = new PropertySet([new("DoesNotExist", 1)]);
        var provider = new TestInstanceContainerProvider(
            Substitute.For<IRunSettings>(), typeActivator, typeof(SharedProbe), new[] { bogusPropertySet }, method);

        var dispatcher = new ClassExecutionDispatcher(engine, typeActivator, mediator);

        var results = await dispatcher.Dispatch(
            typeof(SharedProbe), new[] { provider }, new List<dynamic>(), CancellationToken.None);

        results.Count.ShouldBe(1);
        results[0].IsSuccess.ShouldBeFalse();
        await mediator.Received(1).Publish(
            Arg.Is<TestCaseExceptionNotification>(n => n.TestInstanceContainer == null && n.Exception != null),
            Arg.Any<CancellationToken>());
        await engine.DidNotReceiveWithAnyArgs().ActivateContainer(default!, default, default!, default);
    }
}
