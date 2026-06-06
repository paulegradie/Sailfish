using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Execution.Aggregation;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Handlers.TestCaseEvents;
using Sailfish.TestAdapter.Comparison;
using Shouldly;
using Tests.Common.Builders;
using Tests.Common.Utils;
using Xunit;
using ILogger = Sailfish.Logging.ILogger;

namespace Tests.TestAdapter;

public class TestCaseCompletedNotificationHandlerTests
{
    // The handler now builds a completion message and hands it to the aggregator (which publishes it). A real
    // aggregator sharing the test's mediator lets us observe the published FrameworkTestCaseEndNotification.
    private static TestCompletionAggregator CreateAggregator(IMediator mediator)
    {
        var batchProcessor = new MethodComparisonBatchProcessor(
            Substitute.For<IAdapterSailDiff>(),
            mediator,
            Substitute.For<ILogger>(),
            Substitute.For<ISailDiffUnifiedFormatter>());
        return new TestCompletionAggregator(mediator, batchProcessor, Substitute.For<ILogger>());
    }

    [Fact]
    public async Task TestCaseCompleteNotificationHandlerThrowsOnNullPerformanceTimer()
    {
        var handler = new TestCaseCompletedNotificationHandler(
            Substitute.For<ISailfishConsoleWindowFormatter>(),
            Substitute.For<ISailDiffTestOutputWindowMessageFormatter>(),
            Substitute.For<IRunSettings>(),
            Substitute.For<IMediator>(),
            Substitute.For<IAdapterSailDiff>(),
            Substitute.For<ILogger>(),
            CreateAggregator(Substitute.For<IMediator>())
        );

        var summaryTrackingFormat = new ClassExecutionSummaryTrackingFormat(
            typeof(TestCaseCompletedNotificationHandlerTests),
            new ExecutionSettingsTrackingFormat(),
            new List<CompiledTestCaseResultTrackingFormat> { CompiledTestCaseResultTrackingFormatBuilder.Create().Build() }
        );

        var methodInfo = typeof(TestCaseCompletedNotificationHandlerTests).GetMethods().First();
        var notification = new TestCaseCompletedNotification(
            summaryTrackingFormat,
            new TestInstanceContainerExternal(
                typeof(TestCaseCompletedNotificationHandlerTests),
                new TestCaseCompletedNotificationHandlerTests(),
                methodInfo,
                TestCaseIdBuilder.Create().Build(),
                Substitute.For<IExecutionSettings>(),
                null!,
                false
            ),
            new List<dynamic>()
        );

        await Should.ThrowAsync<SailfishException>(async () => await handler.Handle(notification, CancellationToken.None));
    }

    [Fact]
    public async Task TestCaseCompleteNotificationHandlerThrowsOnNullExternalContainer()
    {
        var handler = new TestCaseCompletedNotificationHandler(
            Substitute.For<ISailfishConsoleWindowFormatter>(),
            Substitute.For<ISailDiffTestOutputWindowMessageFormatter>(),
            Substitute.For<IRunSettings>(),
            Substitute.For<IMediator>(),
            Substitute.For<IAdapterSailDiff>(),
            Substitute.For<ILogger>(),
            CreateAggregator(Substitute.For<IMediator>())
        );

        var summaryTrackingFormat = new ClassExecutionSummaryTrackingFormat(
            typeof(TestCaseCompletedNotificationHandlerTests),
            new ExecutionSettingsTrackingFormat(),
            new List<CompiledTestCaseResultTrackingFormat> { CompiledTestCaseResultTrackingFormatBuilder.Create().Build() }
        );

        var notification = new TestCaseCompletedNotification(
            summaryTrackingFormat,
            null!,
            new List<dynamic>()
        );

        await Should.ThrowAsync<SailfishException>(async () => await handler.Handle(notification, CancellationToken.None));
    }

    [Fact]
    public async Task TestCaseCompleteNotificationHandlerReturnsOnDetectedException()
    {
        var mediatorSub = Substitute.For<IMediator>();
        var handler = new TestCaseCompletedNotificationHandler(
            Substitute.For<ISailfishConsoleWindowFormatter>(),
            Substitute.For<ISailDiffTestOutputWindowMessageFormatter>(),
            Substitute.For<IRunSettings>(),
            mediatorSub,
            Substitute.For<IAdapterSailDiff>(),
            Substitute.For<ILogger>(),
            CreateAggregator(mediatorSub));

        var exMsg = Some.RandomString();
        var summaryTrackingFormat = new ClassExecutionSummaryTrackingFormat(
            typeof(TestCaseCompletedNotificationHandlerTests),
            new ExecutionSettingsTrackingFormat(),
            new List<CompiledTestCaseResultTrackingFormat> { CompiledTestCaseResultTrackingFormatBuilder.Create().WithException(new Exception(exMsg)).Build() }
        );

        var displayName = Some.RandomString();
        var testCaseId = TestCaseIdBuilder.Create().WithTestCaseName(displayName).Build();
        var methodInfo = typeof(TestCaseCompletedNotificationHandlerTests).GetMethods().First();
        var notification = new TestCaseCompletedNotification(
            summaryTrackingFormat,
            new TestInstanceContainerExternal(
                typeof(TestCaseCompletedNotificationHandlerTests),
                new TestCaseCompletedNotificationHandlerTests(),
                methodInfo,
                testCaseId,
                Substitute.For<IExecutionSettings>(),
                new PerformanceTimer(),
                false
            ),
            new List<dynamic>() { new TestCase(testCaseId.DisplayName, new Uri("http://wow.com"), Some.RandomString()) }
        );

        await handler.Handle(notification, CancellationToken.None);

        var notificationResult = mediatorSub.ReceivedCalls()
            .Select(call => call.GetArguments().FirstOrDefault())
            .OfType<FrameworkTestCaseEndNotification>()
            .FirstOrDefault();
        notificationResult.ShouldNotBeNull();
        notificationResult.Exception.ShouldNotBeNull();
        notificationResult.Exception.Message.ShouldBe(exMsg);
    }
}
