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
    private readonly IConsoleWriter _consoleWriter;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IRunSettings _runSettings;
    private readonly ISailDiffConsoleWindowMessageFormatter _sailDiffConsoleWindowMessageFormatter;
    private readonly IStatisticalTestComputer _statisticalTestComputer;

    public SailDiff(IMediator mediator,
        IRunSettings runSettings,
        ILogger logger,
        IStatisticalTestComputer statisticalTestComputer,
        ISailDiffConsoleWindowMessageFormatter sailDiffConsoleWindowMessageFormatter,
        IConsoleWriter consoleWriter)
    {
        _consoleWriter = consoleWriter;
        _logger = logger;
        _mediator = mediator;
        _runSettings = runSettings;
        _sailDiffConsoleWindowMessageFormatter = sailDiffConsoleWindowMessageFormatter;
        _statisticalTestComputer = statisticalTestComputer;
    }

    public void Analyze(TestData beforeData, TestData afterData, SailDiffSettings settings)
    {
        if (beforeData is null || afterData is null)
        {
            _logger.Log(LogLevel.Warning, "Failed to retrieve tracking data... aborting the test operation");
            return;
        }

        var testResults = _statisticalTestComputer.ComputeTest(beforeData, afterData, settings);
        if (!testResults.Any())
        {
            _logger.Log(LogLevel.Information, "No prior test results found for the current set");
            return;
        }

        var testIds = new TestIds(beforeData.TestIds, afterData.TestIds);
        var resultsAsMarkdown = _sailDiffConsoleWindowMessageFormatter.FormConsoleWindowMessageForSailDiff(
            testResults,
            testIds,
            settings,
            CancellationToken.None);

        _logger.Log(LogLevel.Information, resultsAsMarkdown);

        // Publish notification for subscribers (e.g., adapters/formatters)
        _mediator.Publish(new SailDiffAnalysisCompleteNotification(testResults, resultsAsMarkdown), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public async Task Analyze(CancellationToken cancellationToken)
    {
        if (!_runSettings.RunSailDiff) return;
        var beforeAndAfterFileLocations =
            await _mediator.Send(new BeforeAndAfterFileLocationRequest(_runSettings.ProvidedBeforeTrackingFiles), cancellationToken).ConfigureAwait(false);

        if (!beforeAndAfterFileLocations.AfterFilePaths.Any() || !beforeAndAfterFileLocations.BeforeFilePaths.Any())
        {
            var message = new StringBuilder();
            if (!beforeAndAfterFileLocations.BeforeFilePaths.Any()) message.Append("No 'Before' file locations discovered. ");

            if (!beforeAndAfterFileLocations.AfterFilePaths.Any()) message.Append("No 'After' file locations discovered. ");

            message.Append(
                $"If file locations are not provided, data must be provided via the {nameof(ReadInBeforeAndAfterDataRequest)} handler.");
            var msg = message.ToString();
            _logger.Log(LogLevel.Warning, "{Message}", msg);
        }

        var beforeAndAfterData = await _mediator
            .Send(new ReadInBeforeAndAfterDataRequest(beforeAndAfterFileLocations.BeforeFilePaths, beforeAndAfterFileLocations.AfterFilePaths), cancellationToken)
            .ConfigureAwait(false);

        if (beforeAndAfterData.BeforeData is null || beforeAndAfterData.AfterData is null)
        {
            _logger.Log(LogLevel.Warning, "Failed to retrieve tracking data... aborting the test operation");
            return;
        }

        var testResults = _statisticalTestComputer.ComputeTest(
            beforeAndAfterData.BeforeData,
            beforeAndAfterData.AfterData,
            _runSettings.SailDiffSettings);

        if (!testResults.Any())
        {
            _logger.Log(LogLevel.Information, "No prior test results found for the current set");
            return;
        }

        var testIds = new TestIds(beforeAndAfterData.BeforeData.TestIds, beforeAndAfterData.AfterData.TestIds);

        var resultsAsMarkdown = _sailDiffConsoleWindowMessageFormatter.FormConsoleWindowMessageForSailDiff(testResults, testIds, _runSettings.SailDiffSettings, cancellationToken);
        _logger.Log(LogLevel.Information, resultsAsMarkdown);


        await _mediator.Publish(new SailDiffAnalysisCompleteNotification(testResults, resultsAsMarkdown), cancellationToken).ConfigureAwait(false);
    }
}