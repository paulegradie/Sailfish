using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.TestSettingsParser;

namespace Sailfish.TestAdapter.Execution;

internal class TestAdapterExecutionProgram : ITestAdapterExecutionProgram
{
    private readonly ITestAdapterExecutionEngine testAdapterExecutionEngine;
    private readonly IExecutionSummaryWriter executionSummaryWriter;
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;
    private readonly IFileIo fileIo;
    private readonly IAdapterConsoleWriter consoleWriter;
    private readonly ISailDiff sailDiff;
    private readonly IScaleFish scaleFish;

    public TestAdapterExecutionProgram(
        ITestAdapterExecutionEngine testAdapterExecutionEngine,
        IExecutionSummaryWriter executionSummaryWriter,
        ITrackingFileDirectoryReader trackingFileDirectoryReader,
        IFileIo fileIo,
        IAdapterConsoleWriter consoleWriter,
        IAdapterSailDiff sailDiff,
        IAdapterScaleFish scaleFish)
    {
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

        var statisticalTestingEnabled = AnalysisEnabled(out var parsedSettings);
        var preloadedLastRunIfAvailable = new List<DescriptiveStatisticsResult>();

        TestSettings? testSettings = null;
        IRunSettings? runSettings = null;
        string? trackingDir = null;

        if (statisticalTestingEnabled)
        {
            var runSettingsBuilder = RunSettingsBuilder.CreateBuilder();
            if (!string.IsNullOrEmpty(parsedSettings.TestSettings.ResultsDirectory))
            {
                runSettingsBuilder.WithLocalOutputDirectory(parsedSettings.TestSettings.ResultsDirectory);
            }

            testSettings = MapToTestSettings(parsedSettings.TestSettings);
            runSettings = runSettingsBuilder
                .CreateTrackingFiles()
                .WithAnalysis()
                .WithComplexityAnalysis()
                .WithAnalysisTestSettings(testSettings)
                .Build();
            trackingDir = GetRunSettingsTrackingDirectoryPath(runSettings);

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

        var executionSummaries = testAdapterExecutionEngine.Execute(testCases, preloadedLastRunIfAvailable, testSettings, cancellationToken);
        consoleWriter.Present(executionSummaries, new OrderedDictionary());

        if (!statisticalTestingEnabled) return;
        if (parsedSettings.TestSettings.Disabled) return;
        Debug.Assert(trackingDir is not null);
        Debug.Assert(runSettings is not null);

        var timeStamp = DateTime.Now;
        executionSummaryWriter
            .Write(executionSummaries, timeStamp, trackingDir, runSettings, cancellationToken)
            .Wait(cancellationToken);

        scaleFish.Analyze(timeStamp, runSettings, trackingDir, cancellationToken).Wait(cancellationToken);
        sailDiff.Analyze(timeStamp, runSettings, trackingDir, cancellationToken).Wait(cancellationToken);
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

    private static TestSettings MapToTestSettings(SailfishTestSettings settings)
    {
        if (settings?.Resolution is not null)
        {
            // TODO: Modify this when we impl resolution settings throughout (or ditch the idea)
            // settingsBuilder.WithResolution(settings.Resolution);
        }

        var mappedSettings = new TestSettings();
        if (settings?.TestType is not null)
        {
            mappedSettings.SetTestType(settings.TestType);
        }

        if (settings?.UseInnerQuartile is not null)
        {
            mappedSettings.SetUseInnerQuartile(settings.UseInnerQuartile);
        }

        if (settings?.Alpha is not null)
        {
            mappedSettings.SetAlpha(settings.Alpha);
        }

        if (settings?.Round is not null)
        {
            mappedSettings.SetRound(settings.Round);
        }

        return mappedSettings;
    }

    private static bool AnalysisEnabled(out SailfishSettings parsedSettings)
    {
        try
        {
            var settingsFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(
                ".sailfish.json",
                Directory.GetCurrentDirectory(),
                6);
            parsedSettings = SailfishSettingsParser.Parse(settingsFile.FullName);
            return true;
        }
        catch
        {
            parsedSettings = new SailfishSettings();
            return false;
        }
    }
}