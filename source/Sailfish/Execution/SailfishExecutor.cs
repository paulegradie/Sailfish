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
    private readonly IExecutionSummaryCompiler executionSummaryCompiler;
    private readonly IExecutionSummaryWriter executionSummaryWriter;
    private readonly IComplexityComputer complexityComputer;
    private readonly IRunSettings runSettings;
    private readonly ISailFishTestExecutor sailFishTestExecutor;
    private readonly ISailDiff sailDiff;
    private readonly IScaleFish scaleFish;

    public SailfishExecutor(
        ISailFishTestExecutor sailFishTestExecutor,
        ITestCollector testCollector,
        ITestFilter testFilter,
        IExecutionSummaryCompiler executionSummaryCompiler,
        IExecutionSummaryWriter executionSummaryWriter,
        ISailDiff sailDiff,
        IScaleFish scaleFish,
        IComplexityComputer complexityComputer,
        IRunSettings runSettings
    )
    {
        this.sailFishTestExecutor = sailFishTestExecutor;
        this.testCollector = testCollector;
        this.testFilter = testFilter;
        this.executionSummaryCompiler = executionSummaryCompiler;
        this.executionSummaryWriter = executionSummaryWriter;
        this.sailDiff = sailDiff;
        this.scaleFish = scaleFish;
        this.complexityComputer = complexityComputer;
        this.runSettings = runSettings;
    }

    public async Task<SailfishRunResult> Run(CancellationToken cancellationToken)
    {
        var testInitializationResult = CollectTests(runSettings.TestNames, runSettings.TestLocationAnchors.ToArray());
        if (testInitializationResult.IsValid)
        {
            var timeStamp = runSettings.TimeStamp ?? DateTime.Now.ToLocalTime();

            var rawExecutionResults = await sailFishTestExecutor.Execute(testInitializationResult.Tests, cancellationToken);
            var executionSummaries = executionSummaryCompiler.CompileToSummaries(rawExecutionResults, cancellationToken)
                .ToList();

            var executionSummaryTrackingDirectory = GetRunSettingsTrackingDirectoryPath(runSettings, DefaultFileSettings.DefaultExecutionSummaryTrackingDirectory);
            await executionSummaryWriter.Write(
                executionSummaries,
                timeStamp,
                executionSummaryTrackingDirectory,
                runSettings,
                cancellationToken);

            if (runSettings.RunSailDiff)
            {
                await sailDiff.Analyze(timeStamp, runSettings, executionSummaryTrackingDirectory, cancellationToken);
            }

            if (runSettings.RunScalefish)
            {
                await scaleFish.Analyze(timeStamp, runSettings, executionSummaryTrackingDirectory, cancellationToken);
            }

            var exceptions =
                executionSummaries.SelectMany(e => e.CompiledTestCaseResults.SelectMany(c => c.Exceptions));

            return rawExecutionResults.Select(x => x.IsSuccess).All(x => x)
                ? SailfishRunResult.CreateValidResult(executionSummaries)
                : SailfishRunResult.CreateInvalidResult(exceptions);
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

        return SailfishRunResult.CreateInvalidResult(testDiscoveryExceptions);
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