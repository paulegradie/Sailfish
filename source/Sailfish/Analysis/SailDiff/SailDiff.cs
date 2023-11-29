using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;

namespace Sailfish.Analysis.SailDiff;

public interface ISailDiffInternal : IAnalyzeFromFile
{
}

public interface ISailDiff
{
    void Analyze(TestData beforeData, TestData afterData, SailDiffSettings settings);
}

internal class SailDiff : ISailDiffInternal, ISailDiff
{
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;
    private readonly ILogger logger;
    private readonly ITestComputer testComputer;
    private readonly ISailDiffResultMarkdownConverter sailDiffResultMarkdownConverter;
    private readonly IConsoleWriter consoleWriter;

    public SailDiff(
        IMediator mediator,
        IRunSettings runSettings,
        ILogger logger,
        ITestComputer testComputer,
        ISailDiffResultMarkdownConverter sailDiffResultMarkdownConverter,
        IConsoleWriter consoleWriter)
    {
        this.mediator = mediator;
        this.runSettings = runSettings;
        this.logger = logger;
        this.testComputer = testComputer;
        this.sailDiffResultMarkdownConverter = sailDiffResultMarkdownConverter;
        this.consoleWriter = consoleWriter;
    }

    public async Task Analyze(CancellationToken cancellationToken)
    {
        if (!runSettings.RunSailDiff) return;
        var beforeAndAfterFileLocations =
            await mediator.Send(new BeforeAndAfterFileLocationRequest(runSettings.ProvidedBeforeTrackingFiles), cancellationToken).ConfigureAwait(false);

        if (!beforeAndAfterFileLocations.AfterFilePaths.Any() || !beforeAndAfterFileLocations.BeforeFilePaths.Any())
        {
            var message = new StringBuilder();
            if (!beforeAndAfterFileLocations.BeforeFilePaths.Any())
            {
                message.Append("No 'Before' file locations discovered. ");
            }

            if (!beforeAndAfterFileLocations.AfterFilePaths.Any())
            {
                message.Append("No 'After' file locations discovered. ");
            }

            message.Append(
                $"If file locations are not provided, data must be provided via the {nameof(ReadInBeforeAndAfterDataRequest)} handler.");
            var msg = message.ToString();
            logger.Warning("{Message}", msg);
        }

        var beforeAndAfterData = await mediator
            .Send(new ReadInBeforeAndAfterDataRequest(beforeAndAfterFileLocations.BeforeFilePaths, beforeAndAfterFileLocations.AfterFilePaths), cancellationToken)
            .ConfigureAwait(false);

        if (beforeAndAfterData.BeforeData is null || beforeAndAfterData.AfterData is null)
        {
            logger.Warning("Failed to retrieve tracking data... aborting the test operation");
            return;
        }

        var testResults = testComputer.ComputeTest(
            beforeAndAfterData.BeforeData,
            beforeAndAfterData.AfterData,
            runSettings.SailDiffSettings);

        if (!testResults.Any())
        {
            logger.Information("No prior test results found for the current set");
            return;
        }

        var testIds = new TestIds(beforeAndAfterData.BeforeData.TestIds, beforeAndAfterData.AfterData.TestIds);
        var resultsAsMarkdown = sailDiffResultMarkdownConverter.ConvertToMarkdownTable(testResults, testIds, cancellationToken);

        consoleWriter.WriteStatTestResultsToConsole(resultsAsMarkdown, testIds, runSettings.SailDiffSettings);
        await mediator.Publish(new SailDiffAnalysisCompleteNotification(testResults, resultsAsMarkdown), cancellationToken).ConfigureAwait(false);
    }

    public void Analyze(TestData beforeData, TestData afterData, SailDiffSettings settings)
    {
        throw new System.NotImplementedException();
    }
}