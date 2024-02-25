using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Execution;

public interface ITestAdapterExecutionProgram
{
    Task Run(List<TestCase> testCases, CancellationToken cancellationToken);
}

internal class TestAdapterExecutionProgram : ITestAdapterExecutionProgram
{
    // private readonly IExecutionSummaryWriter executionSummaryWriter;
    private readonly IMediator mediator;

    private readonly ILogger logger;
    private readonly ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter;

    // private readonly IRunSettings runSettings;
    // private readonly IAdapterSailDiff sailDiff;
    // private readonly IAdapterScaleFish scaleFish;
    private readonly ITestCaseCountPrinter testCaseCountPrinter;
    private readonly ITestAdapterExecutionEngine testAdapterExecutionEngine;

    public TestAdapterExecutionProgram(
        IRunSettings runSettings,
        ITestAdapterExecutionEngine testAdapterExecutionEngine,
        IExecutionSummaryWriter executionSummaryWriter,
        IMediator mediator,
        ILogger logger,
        ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter,
        // IAdapterSailDiff sailDiff,
        // IAdapterScaleFish scaleFish,
        ITestCaseCountPrinter testCaseCountPrinter)
    {
        this.testAdapterExecutionEngine = testAdapterExecutionEngine;
        this.mediator = mediator;
        this.logger = logger;
        this.sailfishConsoleWindowFormatter = sailfishConsoleWindowFormatter;
        // this.runSettings = runSettings;
        // this.executionSummaryWriter = executionSummaryWriter;
        // this.sailDiff = sailDiff;
        // this.scaleFish = scaleFish;
        this.testCaseCountPrinter = testCaseCountPrinter;
    }

    public async Task Run(List<TestCase> testCases, CancellationToken cancellationToken)
    {
        if (testCases.Count == 0)
        {
            logger.Log(LogLevel.Information, "No Sailfish tests were discovered");
            return;
        }

        testCaseCountPrinter.SetTestCaseTotal(testCases.Count);
        testCaseCountPrinter.PrintDiscoveredTotal();

        var executionSummaries = await testAdapterExecutionEngine.Execute(testCases, cancellationToken);

        // var formattedSailfishResults = sailfishConsoleWindowFormatter.FormConsoleWindowMessageForSailfish(executionSummaries);
        // logger.Log(LogLevel.Information, formattedSailfishResults);

        // await executionSummaryWriter.Write(executionSummaries, cancellationToken);
        await mediator
            .Publish(new TestRunCompletedNotification(executionSummaries.ToTrackingFormat()), cancellationToken)
            .ConfigureAwait(false);

        // if (executionSummaries.SelectMany(x => x.CompiledTestCaseResults.Where(y => y.Exception is not null))
        //     .Any()) return;
        // if (runSettings.DisableAnalysisGlobally) return;
        // if (runSettings.RunSailDiff) await sailDiff.Analyze(cancellationToken);
        // if (runSettings.RunScaleFish) await scaleFish.Analyze(cancellationToken);
    }
}