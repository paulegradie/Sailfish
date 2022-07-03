using System;
using System.Threading.Tasks;
using Sailfish.Presentation;
using Sailfish.Presentation.TTest;
using Sailfish.Statistics;

namespace Sailfish.Execution;

internal class SailfishExecutor
{
    private readonly ITestCollector testCollector;
    private readonly ITestFilter testFilter;
    private readonly IExecutionSummaryCompiler executionSummaryCompiler;
    private readonly ITestResultPresenter testResultPresenter;
    private readonly ISailFishTestExecutor sailFishTestExecutor;

    public SailfishExecutor(
        ISailFishTestExecutor sailFishTestExecutor,
        ITestCollector testCollector,
        ITestFilter testFilter,
        IExecutionSummaryCompiler executionSummaryCompiler,
        ITestResultPresenter testResultPresenter
    )
    {
        this.sailFishTestExecutor = sailFishTestExecutor;
        this.testCollector = testCollector;
        this.testFilter = testFilter;
        this.executionSummaryCompiler = executionSummaryCompiler;
        this.testResultPresenter = testResultPresenter;
    }

    public async Task Run(RunSettings runSettings)
    {
        await Run(
            runSettings.TestNames,
            runSettings.DirectoryPath,
            runSettings.TrackingDirectoryPath,
            runSettings.NoTrack,
            runSettings.Analyze,
            settings: runSettings.Settings,
            notify: runSettings.Notify,
            testLocationTypes: runSettings.TestLocationTypes);
    }

    public async Task Run(
        string[] testNames,
        string directoryPath,
        string trackingDirectory,
        bool noTrack,
        bool analyze,
        bool notify,
        TTestSettings settings,
        params Type[] testLocationTypes)
    {
        var testRun = CollectTests(testNames, testLocationTypes);
        if (testRun.IsValid)
        {
            var timeStamp = DateTime.Now.ToLocalTime();

            var rawExecutionResults = await sailFishTestExecutor.Execute(testRun.Tests);

            var compiledResults = executionSummaryCompiler.CompileToSummaries(rawExecutionResults);

            await testResultPresenter.PresentResults(compiledResults, directoryPath, trackingDirectory, timeStamp, noTrack, analyze, notify, settings);
        }
        else
        {
            Console.WriteLine("\r----------- Error ------------\r");
            foreach (var (reason, names) in testRun.Errors)
            {
                Console.WriteLine(reason);
                foreach (var testName in names) Console.WriteLine($"--- {testName}");
            }
        }
    }

    public TestValidationResult CollectTests(string[] testNames, params Type[] locationTypes)
    {
        var perfTests = testCollector.CollectTestTypes(locationTypes);
        return testFilter.FilterAndValidate(perfTests, testNames);
    }
}