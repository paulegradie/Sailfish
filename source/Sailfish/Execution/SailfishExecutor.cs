using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Presentation;
using Serilog;

namespace Sailfish.Execution;

internal class SailfishExecutor
{
    private readonly ITestCollector testCollector;
    private readonly ITestFilter testFilter;
    private readonly IExecutionSummaryCompiler executionSummaryCompiler;
    private readonly ITestResultPresenter testResultPresenter;
    private readonly ITestResultAnalyzer testResultAnalyzer;
    private readonly IRunSettings runSettings;
    private readonly ISailFishTestExecutor sailFishTestExecutor;

    public SailfishExecutor(
        ISailFishTestExecutor sailFishTestExecutor,
        ITestCollector testCollector,
        ITestFilter testFilter,
        IExecutionSummaryCompiler executionSummaryCompiler,
        ITestResultPresenter testResultPresenter,
        ITestResultAnalyzer testResultAnalyzer,
        IRunSettings runSettings
    )
    {
        this.sailFishTestExecutor = sailFishTestExecutor;
        this.testCollector = testCollector;
        this.testFilter = testFilter;
        this.executionSummaryCompiler = executionSummaryCompiler;
        this.testResultPresenter = testResultPresenter;
        this.testResultAnalyzer = testResultAnalyzer;
        this.runSettings = runSettings;
    }

    public async Task<SailfishRunResult> Run(CancellationToken cancellationToken)
    {
        var testInitializationResult = CollectTests(runSettings.TestNames, runSettings.TestLocationAnchors.ToArray());
        if (testInitializationResult.IsValid)
        {
            var timeStamp = runSettings.TimeStamp ?? DateTime.Now.ToLocalTime();

            var rawExecutionResults = await sailFishTestExecutor.Execute(testInitializationResult.Tests, cancellationToken);
            var executionSummaries = executionSummaryCompiler.CompileToSummaries(rawExecutionResults, cancellationToken).ToList();

            var trackingDir = GetRunSettingsTrackingDirectoryPath(runSettings);
            await testResultPresenter.PresentResults(executionSummaries, timeStamp, trackingDir, runSettings, cancellationToken);
            if (runSettings.Analyze)
            {
                await testResultAnalyzer.Analyze(timeStamp, runSettings, trackingDir, cancellationToken);
            }

            var exceptions = executionSummaries.SelectMany(e => e.CompiledTestCaseResults.SelectMany(c => c.Exceptions));

            return rawExecutionResults.Select(x => x.IsSuccess).All(x => x)
                ? SailfishRunResult.CreateValidResult(executionSummaries)
                : SailfishRunResult.CreateInvalidResult(exceptions);
        }

        Log.Logger.Error("{NumErrors} errors encountered while discovering tests", testInitializationResult.Errors.Count);
        foreach (var (reason, names) in testInitializationResult.Errors)
        {
            Log.Logger.Error("{Reason}", reason);
            foreach (var testName in names) Log.Logger.Error("--- {TestName}", testName);
        }

        return SailfishRunResult.CreateInvalidResult(Enumerable.Empty<Exception>());
    }

    private static string GetRunSettingsTrackingDirectoryPath(IRunSettings runSettings)
    {
        string trackingDirectoryPath;
        if (string.IsNullOrEmpty(runSettings.LocalOutputDirectory) || string.IsNullOrWhiteSpace(runSettings.LocalOutputDirectory))
        {
            trackingDirectoryPath = DefaultFileSettings.DefaultTrackingDirectory;
        }
        else
        {
            trackingDirectoryPath = Path.Join(runSettings.LocalOutputDirectory, DefaultFileSettings.DefaultTrackingDirectory);
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