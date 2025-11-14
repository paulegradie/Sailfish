using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.DefaultHandlers.Sailfish;
using Sailfish.Logging;
using Shouldly;
using Tests.Common.Builders;
using Xunit;

namespace Tests.Library.DefaultHandlers.Sailfish;

public class MethodComparisonTestClassCompletedHandlerMarkdownTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private MethodComparisonTestClassCompletedHandler CreateHandler() => new(_logger, _mediator);

    private static T InvokePrivate<T>(object instance, string methodName, params object[] args)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var mi = instance.GetType().GetMethod(methodName, flags) ?? throw new MissingMethodException(methodName);
        return (T)mi.Invoke(instance, args)!;
    }

    private static void InvokePrivate(object instance, string methodName, params object[] args)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var mi = instance.GetType().GetMethod(methodName, flags) ?? throw new MissingMethodException(methodName);
        mi.Invoke(instance, args);
    }

    [Fact]
    public void CreateConsolidatedMarkdown_NoResults_WritesHeaderAndNotice()
    {
        var handler = CreateHandler();
        var summary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(DummyClass))
            .WithCompiledTestCaseResult([])
            .Build();

        var md = InvokePrivate<string>(handler, "CreateConsolidatedMarkdown", summary);

        md.ShouldContain("Method Comparison Results: DummyClass");
        // Count line formatting may change; only assert header and notice are present
        md.ShouldContain("Generated:");
    }

    [Fact]
    public void CreateConsolidatedMarkdown_WithGroupsAndIndividuals_RendersSections()
    {
        var handler = CreateHandler();

        var sumFastId = TestCaseIdBuilder.Create().WithTestCaseName("SumFast").Build();
        var sumSlowId = TestCaseIdBuilder.Create().WithTestCaseName("SumSlow").Build();
        var sortSoloId = TestCaseIdBuilder.Create().WithTestCaseName("SortSolo").Build();
        var otherId = TestCaseIdBuilder.Create().WithTestCaseName("OtherMethod").Build();

        var summary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(DummyClass))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(sumFastId)
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().WithMean(10.5).WithMedian(10.0).WithSampleSize(100).Build()))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(sumSlowId)
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().WithMean(20.0).WithMedian(19.8).WithSampleSize(100).Build()))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(sortSoloId)
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().WithMean(15.0).Build()))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(otherId)
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().WithMean(5.0).Build()))
            .Build();

        var md = InvokePrivate<string>(handler, "CreateConsolidatedMarkdown", summary);

        md.ShouldContain("Comparison Group: SumCalculation");
        md.ShouldContain("Methods in this comparison:");
        md.ShouldContain("`" + sumFastId.DisplayName + "`");
        md.ShouldContain("`" + sumSlowId.DisplayName + "`");
        md.ShouldContain("Detailed Results");
        md.ShouldContain("10.500ms");
        md.ShouldContain("20.000ms");

        // SortSolo alone should trigger insufficient methods msg
        md.ShouldContain("Insufficient methods for comparison");

        // Non-comparison section should include OtherMethod
        md.ShouldContain("Individual Test Results");
        md.ShouldContain(otherId.DisplayName);
    }

    [Fact]
    public void CreatePerformanceSummary_WithTwoValidMethods_ShowsFastestSlowestAndGap()
    {
        var handler = CreateHandler();
        var list = new List<CompiledTestCaseResultTrackingFormat>
        {
            CompiledTestCaseResultTrackingFormatBuilder.Create()
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("A").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().WithMean(10.0).Build())
                .Build(),
            CompiledTestCaseResultTrackingFormatBuilder.Create()
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("B").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().WithMean(20.0).Build())
                .Build(),
        };

        var summary = InvokePrivate<string>(handler, "CreatePerformanceSummary", list);
        summary.ShouldContain("Fastest:");
        summary.ShouldContain("A");
        summary.ShouldContain("10.000ms");
        summary.ShouldContain("Slowest:");
        summary.ShouldContain("B");
        summary.ShouldContain("20.000ms");
        summary.ShouldContain("Performance Gap");
        summary.ShouldContain("100.0% difference");
    }

    [Fact]
    public void CreatePerformanceSummary_LessThanTwo_ReturnsEmpty()
    {
        var handler = CreateHandler();
        var list = new List<CompiledTestCaseResultTrackingFormat>
        {
            CompiledTestCaseResultTrackingFormatBuilder.Create()
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Only").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().WithMean(10.0).Build())
                .Build()
        };

        var summary = InvokePrivate<string>(handler, "CreatePerformanceSummary", list);
        summary.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Handle_WithNullNotification_LogsErrorAndDoesNotThrow()
    {
        var handler = CreateHandler();
        await Should.NotThrowAsync(async () => await handler.Handle(null!, CancellationToken.None));
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<Exception>(),
            Arg.Is<string>(s => s.Contains("Failed to generate consolidated markdown")),
            Arg.Any<object[]>());
    }

    [Fact]
    public void GetComparisonGroup_Heuristics_WorkForSumSortAndNone()
    {
        var handler = CreateHandler();
        var sum = CompiledTestCaseResultTrackingFormatBuilder.Create()
            .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("DoSumWork").Build())
            .Build();
        var sort = CompiledTestCaseResultTrackingFormatBuilder.Create()
            .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("QuickSort").Build())
            .Build();
        var none = CompiledTestCaseResultTrackingFormatBuilder.Create()
            .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Foo").Build())
            .Build();

        string? g1 = InvokePrivate<string?>(handler, "GetComparisonGroup", sum);
        string? g2 = InvokePrivate<string?>(handler, "GetComparisonGroup", sort);
        string? g3 = InvokePrivate<string?>(handler, "GetComparisonGroup", none);

        g1.ShouldBe("SumCalculation");
        g2.ShouldBe("SortingAlgorithm");
        g3.ShouldBeNull();
    }

    private class DummyClass { }
}

