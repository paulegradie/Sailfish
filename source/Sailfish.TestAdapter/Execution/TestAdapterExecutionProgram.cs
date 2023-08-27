using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Analysis.Saildiff;
using Sailfish.Analysis.Scalefish;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.CsvMaps;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;

namespace Sailfish.TestAdapter.Execution;

internal class TestAdapterExecutionProgram : ITestAdapterExecutionProgram
{
    private readonly IRunSettings runSettings;
    private readonly ITestAdapterExecutionEngine testAdapterExecutionEngine;
    private readonly IExecutionSummaryWriter executionSummaryWriter;
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;
    private readonly IFileIo fileIo;
    private readonly IAdapterConsoleWriter consoleWriter;
    private readonly ISailDiff sailDiff;
    private readonly IScaleFish scaleFish;

    public TestAdapterExecutionProgram(
        IRunSettings runSettings,
        ITestAdapterExecutionEngine testAdapterExecutionEngine,
        IExecutionSummaryWriter executionSummaryWriter,
        ITrackingFileDirectoryReader trackingFileDirectoryReader,
        IFileIo fileIo,
        IAdapterConsoleWriter consoleWriter,
        IAdapterSailDiff sailDiff,
        IAdapterScaleFish scaleFish)
    {
        this.runSettings = runSettings;
        this.testAdapterExecutionEngine = testAdapterExecutionEngine;
        this.executionSummaryWriter = executionSummaryWriter;
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
        this.fileIo = fileIo;
        this.consoleWriter = consoleWriter;
        this.sailDiff = sailDiff;
        this.scaleFish = scaleFish;
    }

    public void Run(List<TestCase> testCases, CancellationToken cancellationToken)
    {
        if (testCases.Count == 0)
        {
            consoleWriter.WriteString("No Sailfish tests were discovered");
            return;
        }

        var timeStamp = DateTime.Now;
        var trackingDir = GetRunSettingsTrackingDirectoryPath(runSettings);
        var preloadedLastRunIfAvailable = new List<DescriptiveStatisticsResult>();
        if (!runSettings.DisableAnalysisGlobally && (runSettings.RunScalefish || runSettings.RunSailDiff))
        {
            var trackingFiles = trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(trackingDir, ascending: false);
            var latestRun = trackingFiles.Count switch
            {
                0 => null,
                1 => trackingFiles.Single(),
                _ => trackingFiles.First()
            };

            if (latestRun is not null)
            {
                var data = fileIo
                    .ReadCsvFile<DescriptiveStatisticsResultCsvMap, DescriptiveStatisticsResult>(
                        latestRun,
                        cancellationToken)
                    .GetAwaiter()
                    .GetResult();
                preloadedLastRunIfAvailable.AddRange(data);
            }
        }

        var executionSummaries = testAdapterExecutionEngine.Execute(testCases, preloadedLastRunIfAvailable, runSettings.Settings, cancellationToken);
        consoleWriter.Present(executionSummaries, new OrderedDictionary());
        executionSummaryWriter
            .Write(executionSummaries, timeStamp, trackingDir, runSettings, cancellationToken)
            .Wait(cancellationToken);

        if (runSettings.DisableAnalysisGlobally) return;
        if (runSettings.RunSailDiff)
        {
            sailDiff.Analyze(timeStamp, runSettings, trackingDir, cancellationToken).Wait(cancellationToken);
        }
        if (runSettings.RunScalefish)
        {
            scaleFish.Analyze(timeStamp, runSettings, trackingDir, cancellationToken).Wait(cancellationToken);
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