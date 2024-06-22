using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.TestAdapter.Execution;
using Shouldly;
using Tests.Common.Builders;
using Tests.Common.Builders.ScaleFish;
using Tests.Common.Utils;
using Xunit;

namespace Tests.TestAdapter;

public class AdapterScaleFishTests
{
    [Fact]
    public async Task AnalyzeReturnsWhenDisabled()
    {
        var mediator = Substitute.For<IMediator>();
        var runSettings = Substitute.For<IRunSettings>();
        var computer = Substitute.For<IComplexityComputer>();
        var converter = Substitute.For<IMarkdownTableConverter>();
        var logger = Substitute.For<ILogger>();
        runSettings.RunScaleFish.Returns(false);

        var scaleFish = new AdapterScaleFish(mediator, runSettings, computer, converter, logger);
        await scaleFish.Analyze(CancellationToken.None);

        mediator.ReceivedCalls().Count().ShouldBe(0);
    }

    [Fact]
    public async Task AnalyzeReturnsWhenNoSummariesAreFound()
    {
        var mediator = Substitute.For<IMediator>();
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.RunScaleFish.Returns(true);

        var computer = Substitute.For<IComplexityComputer>();
        var converter = Substitute.For<IMarkdownTableConverter>();
        var logger = Substitute.For<ILogger>();
        var response = new GetLatestExecutionSummaryResponse(new List<IClassExecutionSummary>());

        mediator.Send(new GetLatestExecutionSummaryRequest(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(response);

        var scaleFish = new AdapterScaleFish(mediator, runSettings, computer, converter, logger);
        await scaleFish.Analyze(CancellationToken.None);

        computer.AnalyzeComplexity(Arg.Any<List<IClassExecutionSummary>>()).ReceivedCalls().Count().ShouldBe(0);
    }


    [Fact]
    public async Task AnalyzeReturnsWhenNoComplexityResultsAreFound()
    {
        var mediator = Substitute.For<IMediator>();
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.RunScaleFish.Returns(true);

        var computer = Substitute.For<IComplexityComputer>();
        computer.AnalyzeComplexity(Arg.Any<List<IClassExecutionSummary>>()).ReturnsForAnyArgs([]);

        var converter = Substitute.For<IMarkdownTableConverter>();
        converter.ConvertScaleFishResultToMarkdown(Arg.Any<List<ScalefishClassModel>>()).ReturnsForAnyArgs(string.Empty);

        var logger = Substitute.For<ILogger>();

        var testCaseId = Some.SimpleTestCaseId();
        var result = PerformanceRunResultBuilder.Create().WithDisplayName(testCaseId.DisplayName).Build();
        var compiledTestResult = new CompiledTestCaseResult(testCaseId, Some.RandomString(), result);
        var summary = new ClassExecutionSummary(typeof(AdapterScaleFishTests), new ExecutionSettings(), new[] { compiledTestResult });

        var response = new GetLatestExecutionSummaryResponse([summary]);

        mediator.Send(new GetLatestExecutionSummaryRequest(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(response);

        var scaleFish = new AdapterScaleFish(mediator, runSettings, computer, converter, logger);
        await scaleFish.Analyze(CancellationToken.None);

        converter.ReceivedCalls().Count().ShouldBe(0);
    }

    [Fact]
    public async Task AnalyzePublishesComplexityMarkdownAndResults()
    {
        var mediator = Substitute.For<IMediator>();
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.RunScaleFish.Returns(true);

        var propertyModel = ScaleFishPropertyModelBuilder.Create().Build();
        var methodModel = new ScaleFishMethodModel(Some.RandomString(), [propertyModel]);
        var classModel = new ScalefishClassModel(Some.RandomString(), Some.RandomString(), [methodModel]);

        var computer = Substitute.For<IComplexityComputer>();
        computer.AnalyzeComplexity(Arg.Any<List<IClassExecutionSummary>>()).ReturnsForAnyArgs([classModel]);

        var converter = Substitute.For<IMarkdownTableConverter>();
        converter.ConvertScaleFishResultToMarkdown(Arg.Any<List<ScalefishClassModel>>()).ReturnsForAnyArgs(string.Empty);

        var logger = Substitute.For<ILogger>();

        var testCaseId = Some.SimpleTestCaseId();
        var result = PerformanceRunResultBuilder.Create().WithDisplayName(testCaseId.DisplayName).Build();
        var compiledTestResult = new CompiledTestCaseResult(testCaseId, Some.RandomString(), result);
        var summary = new ClassExecutionSummary(typeof(AdapterScaleFishTests), new ExecutionSettings(), new[] { compiledTestResult });

        var response = new GetLatestExecutionSummaryResponse([summary]);

        mediator.Send(new GetLatestExecutionSummaryRequest(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(response);

        var scaleFish = new AdapterScaleFish(mediator, runSettings, computer, converter, logger);
        await scaleFish.Analyze(CancellationToken.None);

        converter.ReceivedCalls().Count().ShouldBe(1);
        var notification = mediator.ReceivedCalls().Last().GetArguments().First();
        if (notification is null) Assert.Fail();
        notification.GetType().ShouldBe(typeof(ScaleFishAnalysisCompleteNotification));
        var typedNotification = notification as ScaleFishAnalysisCompleteNotification;
        if (typedNotification is null) Assert.Fail();
        typedNotification.TestClassComplexityResults.ShouldBeEquivalentTo(new List<ScalefishClassModel> { classModel });
    }

    [Fact]
    public async Task AnalyzerExceptionsAreSwallowed()
    {
        var mediator = Substitute.For<IMediator>();
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.RunScaleFish.Returns(true);

        var computer = Substitute.For<IComplexityComputer>();
        computer.AnalyzeComplexity(Arg.Any<List<IClassExecutionSummary>>()).ThrowsForAnyArgs(new Exception("Test"));

        var converter = Substitute.For<IMarkdownTableConverter>();
        converter.ConvertScaleFishResultToMarkdown(Arg.Any<List<ScalefishClassModel>>()).ReturnsForAnyArgs(string.Empty);

        var logger = Substitute.For<ILogger>();

        var testCaseId = Some.SimpleTestCaseId();
        var result = PerformanceRunResultBuilder.Create().WithDisplayName(testCaseId.DisplayName).Build();
        var compiledTestResult = new CompiledTestCaseResult(testCaseId, Some.RandomString(), result);
        var summary = new ClassExecutionSummary(typeof(AdapterScaleFishTests), new ExecutionSettings(), new[] { compiledTestResult });

        var response = new GetLatestExecutionSummaryResponse([summary]);

        mediator.Send(new GetLatestExecutionSummaryRequest(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(response);

        var scaleFish = new AdapterScaleFish(mediator, runSettings, computer, converter, logger);
        await scaleFish.Analyze(CancellationToken.None);

        logger.ReceivedCalls().Count().ShouldBe(1);
    }
}