using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;

namespace Sailfish.Analysis.SailDiff;

public interface ISailDiffInternal : IAnalyzeFromFile;

public interface ISailDiff
{
    void Analyze(TestData beforeData, TestData afterData, SailDiffSettings settings);
}

internal class SailDiff : ISailDiffInternal, ISailDiff
{
    private readonly IConsoleWriter consoleWriter;
    private readonly ILogger logger;
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;
    private readonly ISailDiffConsoleWindowMessageFormatter sailDiffConsoleWindowMessageFormatter;
    private readonly IStatisticalTestComputer statisticalTestComputer;

    public SailDiff(IMediator mediator,
        IRunSettings runSettings,
        ILogger logger,
        IStatisticalTestComputer statisticalTestComputer,
        ISailDiffConsoleWindowMessageFormatter sailDiffConsoleWindowMessageFormatter,
        IConsoleWriter consoleWriter)
    {
        this.consoleWriter = consoleWriter;
        this.logger = logger;
        this.mediator = mediator;
        this.runSettings = runSettings;
        this.sailDiffConsoleWindowMessageFormatter = sailDiffConsoleWindowMessageFormatter;
        this.statisticalTestComputer = statisticalTestComputer;
    }

    public void Analyze(TestData beforeData, TestData afterData, SailDiffSettings settings)
    {
        if (beforeData is null || afterData is null)
        {
            logger.Log(LogLevel.Warning, "Failed to retrieve tracking data... aborting the test operation");
            return;
        }

        var testResults = statisticalTestComputer.ComputeTest(beforeData, afterData, settings);
        if (!testResults.Any())
        {
            logger.Log(LogLevel.Information, "No prior test results found for the current set");
            return;
        }

        var testIds = new TestIds(beforeData.TestIds, afterData.TestIds);
        var resultsAsMarkdown = sailDiffConsoleWindowMessageFormatter.FormConsoleWindowMessageForSailDiff(
            testResults,
            testIds,
            settings,
            CancellationToken.None);

        logger.Log(LogLevel.Information, resultsAsMarkdown);

        // Publish notification for subscribers (e.g., adapters/formatters)
        mediator.Publish(new SailDiffAnalysisCompleteNotification(testResults, resultsAsMarkdown), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public async Task Analyze(CancellationToken cancellationToken)
    {
        if (!runSettings.RunSailDiff) return;
        var beforeAndAfterFileLocations =
            await mediator.Send(new BeforeAndAfterFileLocationRequest(runSettings.ProvidedBeforeTrackingFiles), cancellationToken).ConfigureAwait(false);

        if (!beforeAndAfterFileLocations.AfterFilePaths.Any() || !beforeAndAfterFileLocations.BeforeFilePaths.Any())
        {
            var message = new StringBuilder();
            if (!beforeAndAfterFileLocations.BeforeFilePaths.Any()) message.Append("No 'Before' file locations discovered. ");

            if (!beforeAndAfterFileLocations.AfterFilePaths.Any()) message.Append("No 'After' file locations discovered. ");

            message.Append(
                $"If file locations are not provided, data must be provided via the {nameof(ReadInBeforeAndAfterDataRequest)} handler.");
            var msg = message.ToString();
            logger.Log(LogLevel.Warning, "{Message}", msg);
        }

        var beforeAndAfterData = await mediator
            .Send(new ReadInBeforeAndAfterDataRequest(beforeAndAfterFileLocations.BeforeFilePaths, beforeAndAfterFileLocations.AfterFilePaths), cancellationToken)
            .ConfigureAwait(false);

        if (beforeAndAfterData.BeforeData is null || beforeAndAfterData.AfterData is null)
        {
            logger.Log(LogLevel.Warning, "Failed to retrieve tracking data... aborting the test operation");
            return;
        }

        var testResults = statisticalTestComputer.ComputeTest(
            beforeAndAfterData.BeforeData,
            beforeAndAfterData.AfterData,
            runSettings.SailDiffSettings);

        if (!testResults.Any())
        {
            logger.Log(LogLevel.Information, "No prior test results found for the current set");
            return;
        }

        var testIds = new TestIds(beforeAndAfterData.BeforeData.TestIds, beforeAndAfterData.AfterData.TestIds);

        var resultsAsMarkdown = sailDiffConsoleWindowMessageFormatter.FormConsoleWindowMessageForSailDiff(testResults, testIds, runSettings.SailDiffSettings, cancellationToken);
        logger.Log(LogLevel.Information, resultsAsMarkdown);


        await mediator.Publish(new SailDiffAnalysisCompleteNotification(testResults, resultsAsMarkdown), cancellationToken).ConfigureAwait(false);
    }
}