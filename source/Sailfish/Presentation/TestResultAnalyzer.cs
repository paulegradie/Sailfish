﻿using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Serilog;

namespace Sailfish.Presentation;

public class TestResultAnalyzer : ITestResultAnalyzer
{
    private readonly IMediator mediator;
    private readonly ILogger logger;
    private readonly ITestComputer testComputer;
    private readonly ITestResultTableContentFormatter testResultTableContentFormatter;

    public TestResultAnalyzer(
        IMediator mediator,
        ILogger logger,
        ITestComputer testComputer,
        ITestResultTableContentFormatter testResultTableContentFormatter
    )
    {
        this.mediator = mediator;
        this.logger = logger;
        this.testComputer = testComputer;
        this.testResultTableContentFormatter = testResultTableContentFormatter;
    }

    public async Task Analyze(
        DateTime timeStamp,
        RunSettings runSettings,
        string trackingDir,
        CancellationToken cancellationToken
    )
    {
        var beforeAndAfterFileLocations = await mediator.Send(
                new BeforeAndAfterFileLocationCommand(
                    trackingDir,
                    runSettings.Tags,
                    runSettings.BeforeTarget,
                    runSettings.Args),
                cancellationToken)
            .ConfigureAwait(false);

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

            message.Append($"If file locations are not provided, data must be provided via the {nameof(ReadInBeforeAndAfterDataCommand)} handler.");
            var msg = message.ToString();
            logger.Warning("{Message}", msg);
        }

        var beforeAndAfterData = await mediator.Send(
                new ReadInBeforeAndAfterDataCommand(
                    beforeAndAfterFileLocations.BeforeFilePaths,
                    beforeAndAfterFileLocations.AfterFilePaths,
                    runSettings.BeforeTarget,
                    runSettings.Tags,
                    runSettings.Args),
                cancellationToken)
            .ConfigureAwait(false);

        if (beforeAndAfterData.BeforeData is null || beforeAndAfterData.AfterData is null)
        {
            logger.Warning("Failed to retrieve tracking data... aborting the test operation");
            return;
        }

        var testResults = testComputer.ComputeTest(
            beforeAndAfterData.BeforeData,
            beforeAndAfterData.AfterData,
            runSettings.Settings);

        if (!testResults.Any())
        {
            logger.Information("No prior test results found for the current set");
            return;
        }

        var testIds = new TestIds(beforeAndAfterData.BeforeData.TestIds, beforeAndAfterData.AfterData.TestIds);
        var testResultFormats = testResultTableContentFormatter.CreateTableFormats(testResults, testIds, cancellationToken);
        TestResultConsoleWriter.WriteToConsole(testResultFormats.MarkdownFormat, testIds, runSettings.Settings);

        await mediator.Publish(
                new WriteTestResultsAsMarkdownCommand(
                    testResultFormats.MarkdownFormat,
                    runSettings.DirectoryPath,
                    runSettings.Settings,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args),
                cancellationToken)
            .ConfigureAwait(false);

        await mediator.Publish(
                new WriteTestResultsAsCsvCommand(
                    testResultFormats.CsvFormat,
                    runSettings.DirectoryPath,
                    runSettings.Settings,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args),
                cancellationToken)
            .ConfigureAwait(false);

        if (runSettings.Notify)
        {
            await mediator.Publish(
                    new NotifyOnTestResultCommand(
                        testResultFormats,
                        runSettings.Settings,
                        timeStamp,
                        runSettings.Tags,
                        runSettings.Args),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}