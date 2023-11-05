using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;

namespace Sailfish.TestAdapter.Execution;

internal class TestAdapterExecutionProgram : ITestAdapterExecutionProgram
{
    private readonly IRunSettings runSettings;
    private readonly ITestAdapterExecutionEngine testAdapterExecutionEngine;
    private readonly IExecutionSummaryWriter executionSummaryWriter;
    private readonly IMediator mediator;
    private readonly IAdapterConsoleWriter consoleWriter;
    private readonly IAdapterSailDiff sailDiff;
    private readonly IAdapterScaleFish scaleFish;

    public TestAdapterExecutionProgram(
        IRunSettings runSettings,
        ITestAdapterExecutionEngine testAdapterExecutionEngine,
        IExecutionSummaryWriter executionSummaryWriter,
        IMediator mediator,
        IAdapterConsoleWriter consoleWriter,
        IAdapterSailDiff sailDiff,
        IAdapterScaleFish scaleFish)
    {
        this.runSettings = runSettings;
        this.testAdapterExecutionEngine = testAdapterExecutionEngine;
        this.executionSummaryWriter = executionSummaryWriter;
        this.mediator = mediator;
        this.consoleWriter = consoleWriter;
        this.sailDiff = sailDiff;
        this.scaleFish = scaleFish;
    }

    public async Task Run(List<TestCase> testCases, CancellationToken cancellationToken)
    {
        if (testCases.Count == 0)
        {
            consoleWriter.WriteString("No Sailfish tests were discovered");
            return;
        }

        var preloadedLastRunsIfAvailable = new TrackingFileDataList();
        if (!runSettings.DisableAnalysisGlobally && (runSettings.RunScalefish || runSettings.RunSailDiff))
        {
            try
            {
                var response = await mediator.Send(new GetAllTrackingDataOrderedChronologicallyRequest(false), cancellationToken);
                preloadedLastRunsIfAvailable.AddRange(response.TrackingData);
            }
            catch (Exception ex)
            {
                consoleWriter.WriteString(ex.Message);
            }
        }

        var executionSummaries = await testAdapterExecutionEngine.Execute(testCases, preloadedLastRunsIfAvailable, cancellationToken);

        // Something weird is going on here when there is an exception - all of the testcases runs get logged into the test output window for the errored case
        consoleWriter.WriteToConsole(executionSummaries, new OrderedDictionary());

        await executionSummaryWriter.Write(executionSummaries, cancellationToken);
        await mediator.Publish(new TestRunCompletedNotification(executionSummaries.ToTrackingFormat()), cancellationToken).ConfigureAwait(false);

        if (executionSummaries.SelectMany(x => x.CompiledTestCaseResults.Where(y => y.Exception is not null)).Any()) return;
        if (runSettings.DisableAnalysisGlobally) return;
        if (runSettings.RunSailDiff) await sailDiff.Analyze(cancellationToken);
        if (runSettings.RunScalefish) await scaleFish.Analyze(cancellationToken);
    }
}