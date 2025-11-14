using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using Sailfish.Attributes;
using Sailfish.DefaultHandlers.Sailfish;
using Sailfish.Diagnostics.Environment;
using Sailfish.Logging;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Contracts.Public.Notifications;

using Shouldly;
using Tests.Common.Builders;
using Xunit;

namespace Tests.Library.DefaultHandlers.Sailfish;

public class MethodComparisonTestRunCompletedHandlerHealthSectionTests
{
    private sealed class StubProvider : IEnvironmentHealthReportProvider
    {
        public EnvironmentHealthReport? Current { get; set; }
    }

    private static TestRunCompletedNotification CreateNotificationWithWriteToMarkdown()
    {
        var summary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithWriteToMarkdown))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("M1").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().WithMean(1.0).Build()))
            .Build();
        return new TestRunCompletedNotification(new List<ClassExecutionSummaryTrackingFormat> { summary });
    }

    [WriteToMarkdown]
    private class TestClassWithWriteToMarkdown { }

    [Fact]
    public async Task Markdown_Includes_EnvironmentHealth_Section_When_Report_Present()
    {
        var logger = Substitute.For<ILogger>();
        var mediator = Substitute.For<IMediator>();
        var provider = new StubProvider
        {
            Current = new EnvironmentHealthReport(new List<HealthCheckEntry>
            {
                new("Process Priority", HealthStatus.Pass, "High"),
                new("GC Mode", HealthStatus.Warn, "Workstation"),
            })
        };

        var handler = new MethodComparisonTestRunCompletedHandler(logger, mediator, provider);
        var notification = CreateNotificationWithWriteToMarkdown();

        await handler.Handle(notification, CancellationToken.None);

        await mediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonMarkdownNotification>(n =>
                n.MarkdownContent.Contains("Environment Health Check") &&
                n.MarkdownContent.Contains("Score:") &&
                n.MarkdownContent.Contains("Process Priority")
            ),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Markdown_Excludes_EnvironmentHealth_Section_When_Report_Null()
    {
        var logger = Substitute.For<ILogger>();
        var mediator = Substitute.For<IMediator>();
        var provider = new StubProvider { Current = null };

        var handler = new MethodComparisonTestRunCompletedHandler(logger, mediator, provider);
        var notification = CreateNotificationWithWriteToMarkdown();

        await handler.Handle(notification, CancellationToken.None);

        await mediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonMarkdownNotification>(n =>
                !n.MarkdownContent.Contains("Environment Health Check")),
            Arg.Any<CancellationToken>());
    }
}

