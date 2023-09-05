using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Private;
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

        var timeStamp = DateTime.Now;
        var trackingDir = GetRunSettingsTrackingDirectoryPath(runSettings);
        var preloadedLastRunsIfAvailable = new TrackingFileDataList();
        if (!runSettings.DisableAnalysisGlobally && (runSettings.RunScalefish || runSettings.RunSailDiff))
        {
            var response = await mediator.Send(new SailfishGetAllTrackingDataOrderedChronologicallyRequest(trackingDir, false), cancellationToken);
            preloadedLastRunsIfAvailable.AddRange(response.TrackingData);
        }

        var executionSummaries = await testAdapterExecutionEngine.Execute(testCases, preloadedLastRunsIfAvailable, runSettings.Settings, cancellationToken);
        consoleWriter.Present(executionSummaries, new OrderedDictionary());
        await executionSummaryWriter.Write(executionSummaries, timeStamp, trackingDir, runSettings, cancellationToken);

        if (runSettings.DisableAnalysisGlobally) return;
        if (runSettings.RunSailDiff)
        {
            await sailDiff.Analyze(timeStamp, runSettings, trackingDir, cancellationToken);
        }

        if (runSettings.RunScalefish)
        {
            await scaleFish.Analyze(timeStamp, runSettings, trackingDir, cancellationToken);
        }
    }

    private static string GetRunSettingsTrackingDirectoryPath(IRunSettings runSettings)
    {
        string trackingDirectoryPath;
        if (string.IsNullOrEmpty(runSettings.LocalOutputDirectory) ||
            string.IsNullOrWhiteSpace(runSettings.LocalOutputDirectory))
        {
            trackingDirectoryPath = DefaultFileSettings.DefaultExecutionSummaryTrackingDirectory;
        }
        else
        {
            trackingDirectoryPath = Path.Join(runSettings.LocalOutputDirectory, DefaultFileSettings.DefaultExecutionSummaryTrackingDirectory);
        }

        if (!Directory.Exists(trackingDirectoryPath))
        {
            Directory.CreateDirectory(trackingDirectoryPath);
        }

        return trackingDirectoryPath;
    }
}