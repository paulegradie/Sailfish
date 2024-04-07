using MediatR;
using NSubstitute;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.TestAdapter.Execution;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        var computer = Substitute.For<IComplexityComputer>();
        var converter = Substitute.For<IMarkdownTableConverter>();
        var logger = Substitute.For<ILogger>();
        var response = new GetLatestExecutionSummaryResponse(new List<IClassExecutionSummary>());

        mediator.Send(new GetLatestExecutionSummaryRequest(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(response);

        var scaleFish = new AdapterScaleFish(mediator, runSettings, computer, converter, logger);
        await scaleFish.Analyze(CancellationToken.None);

        computer.AnalyzeComplexity(Arg.Any<List<IClassExecutionSummary>>()).ReceivedCalls().Count().ShouldBe(0);
    }
}