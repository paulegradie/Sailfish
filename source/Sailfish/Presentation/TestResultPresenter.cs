﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Accord.Collections;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation.Csv;
using Sailfish.Presentation.TTest;
using Sailfish.Statistics;
using Sailfish.Statistics.StatisticalAnalysis;

namespace Sailfish.Presentation;

internal class TestResultPresenter : ITestResultPresenter
{
    private readonly IMediator mediator;
    private readonly IPerformanceCsvTrackingWriter performanceCsvTrackingWriter;
    private readonly ITwoTailedTTestWriter twoTailedTTestWriter;

    public TestResultPresenter(
        IMediator mediator,
        IPerformanceCsvTrackingWriter performanceCsvTrackingWriter,
        ITwoTailedTTestWriter twoTailedTTestWriter)
    {
        this.mediator = mediator;
        this.performanceCsvTrackingWriter = performanceCsvTrackingWriter;
        this.twoTailedTTestWriter = twoTailedTTestWriter;
    }

    public async Task PresentResults(
        List<ExecutionSummary> resultContainers,
        string directoryPath,
        string trackingDirectory,
        DateTime timeStamp,
        bool noTrack,
        bool analyze,
        bool notify,
        TTestSettings testSettings,
        OrderedDictionary<string, string> tags)
    {
        await mediator.Publish(new WriteToConsoleCommand(resultContainers, tags));
        await mediator.Publish(new WriteToMarkDownCommand(resultContainers, directoryPath, timeStamp, tags));
        await mediator.Publish(new WriteToCsvCommand(resultContainers, directoryPath, timeStamp, tags));

        var trackingDir = string.IsNullOrEmpty(trackingDirectory) ? Path.Combine(directoryPath, "tracking_output") : trackingDirectory;
        if (!noTrack)
        {
            var trackingContent = await performanceCsvTrackingWriter.ConvertToCsvStringContent(resultContainers);
            await mediator.Publish(
                new WriteCurrentTrackingFileCommand(
                    trackingContent,
                    trackingDir,
                    timeStamp,
                    tags));
        }

        if (analyze)
        {
            var response = await mediator.Send(new BeforeAndAfterFileLocationCommand(trackingDir, tags));
            if (string.IsNullOrEmpty(response.BeforeFilePath) || string.IsNullOrEmpty(response.AfterFilePath)) return;

            var tTestFormats = await twoTailedTTestWriter.ComputeAndConvertToStringContent(new BeforeAndAfterTrackingFiles(response.BeforeFilePath, response.AfterFilePath), testSettings);
            await mediator.Publish(new WriteTTestResultAsMarkdownCommand(tTestFormats.MarkdownTable, directoryPath, testSettings, timeStamp, tags));
            await mediator.Publish(new WriteTTestResultAsCsvCommand(tTestFormats.CsvRows, directoryPath, testSettings, timeStamp, tags));

            if (notify)
            {
                await mediator.Publish(new NotifyOnTestResultCommand(tTestFormats, testSettings, timeStamp, tags));
            }
        }
    }
}