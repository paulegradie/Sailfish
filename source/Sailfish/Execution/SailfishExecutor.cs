using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Presentation;
using Serilog;

namespace Sailfish.Execution;

internal class SailfishExecutor
{
    private readonly ITestCollector testCollector;
    private readonly ITestFilter testFilter;
    private readonly IClassExecutionSummaryCompiler classExecutionSummaryCompiler;
    private readonly IExecutionSummaryWriter executionSummaryWriter;
    private readonly IRunSettings runSettings;
    private readonly ISailFishTestExecutor sailFishTestExecutor;
    private readonly ISailDiff sailDiff;
    private readonly IScaleFish scaleFish;

    public SailfishExecutor(
        ISailFishTestExecutor sailFishTestExecutor,
        ITestCollector testCollector,
        ITestFilter testFilter,
        IClassExecutionSummaryCompiler classExecutionSummaryCompiler,
        IExecutionSummaryWriter executionSummaryWriter,
        ISailDiff sailDiff,
        IScaleFish scaleFish,
        IRunSettings runSettings
    )
    {
        this.sailFishTestExecutor = sailFishTestExecutor;
        this.testCollector = testCollector;
        this.testFilter = testFilter;
        this.classExecutionSummaryCompiler = classExecutionSummaryCompiler;
        this.executionSummaryWriter = executionSummaryWriter;
        this.sailDiff = sailDiff;
        this.scaleFish = scaleFish;
        this.runSettings = runSettings;
    }

    public async Task<SailfishRunResult> Run(CancellationToken cancellationToken)
    {
        var testInitializationResult = CollectTests(runSettings.TestNames, runSettings.TestLocationAnchors.ToArray());
        if (testInitializationResult.IsValid)
        {
            var timeStamp = runSettings.TimeStamp ?? DateTime.Now.ToLocalTime();

            var testClassResultGroups = await sailFishTestExecutor.Execute(testInitializationResult.Tests, cancellationToken);
            var classExecutionSummaries = classExecutionSummaryCompiler.CompileToSummaries(testClassResultGroups, cancellationToken)
                .ToList();

            var trackingDirectory = runSettings.GetRunSettingsTrackingDirectoryPath();
            await executionSummaryWriter.Write(classExecutionSummaries, timeStamp, cancellationToken);

            if (runSettings.RunSailDiff)
            {
                await sailDiff.Analyze(timeStamp, cancellationToken);
            }

            if (runSettings.RunScalefish)
            {
                await scaleFish.Analyze(timeStamp, cancellationToken);
            }

            var exceptions = classExecutionSummaries
                .SelectMany(classExecutionSummary =>
                    classExecutionSummary
                        .CompiledTestCaseResults
                        .Where(e => e.Exception is not null)
                        .Select(c => c.Exception))
                .Cast<Exception>()
                .ToList();

            return SailfishRunResult.CreateResult(classExecutionSummaries, exceptions);
        }

        Log.Logger.Error("{NumErrors} errors encountered while discovering tests",
            testInitializationResult.Errors.Count);

        var testDiscoveryExceptions = new List<Exception>();
        foreach (var (reason, names) in testInitializationResult.Errors)
        {
            Log.Logger.Error("{Reason}", reason);
            foreach (var testName in names)
            {
                Log.Logger.Error("--- {TestName}", testName);
                testDiscoveryExceptions.Add(new Exception($"Test: {testName} - Error: {reason}"));
            }
        }

        return SailfishRunResult.CreateResult(Array.Empty<IClassExecutionSummary>(), testDiscoveryExceptions);
    }

    private static string GetRunSettingsTrackingDirectoryPath(IRunSettings runSettings, string defaultDirectory)
    {
        string trackingDirectoryPath;
        if (string.IsNullOrEmpty(runSettings.LocalOutputDirectory) ||
            string.IsNullOrWhiteSpace(runSettings.LocalOutputDirectory))
        {
            trackingDirectoryPath = defaultDirectory;
        }
        else
        {
            trackingDirectoryPath =
                Path.Join(runSettings.LocalOutputDirectory, defaultDirectory);
        }

        if (!Directory.Exists(trackingDirectoryPath))
        {
            Directory.CreateDirectory(trackingDirectoryPath);
        }

        return trackingDirectoryPath;
    }

    private TestInitializationResult CollectTests(IEnumerable<string> testNames, IEnumerable<Type> locationTypes)
    {
        var perfTests = testCollector.CollectTestTypes(locationTypes);
        return testFilter.FilterAndValidate(perfTests, testNames);
    }
}