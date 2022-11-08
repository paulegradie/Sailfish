using System;
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

    public async Task<SailfishValidity> Run(RunSettings runSettings, CancellationToken cancellationToken)
    {
        return await Run(
            testNames: runSettings.TestNames,
            runSettings: runSettings,
            cancellationToken: cancellationToken,
            testLocationTypes: runSettings.TestLocationTypes
        );
    }

    private async Task<SailfishValidity> Run(
        string[] testNames,
        RunSettings runSettings,
        CancellationToken cancellationToken,
        params Type[] testLocationTypes)
    {
        var testRun = CollectTests(testNames, testLocationTypes);
        if (testRun.IsValid)
        {
            var timeStamp = runSettings.TimeStamp ?? DateTime.Now.ToLocalTime();

            var rawExecutionResults = await sailFishTestExecutor.Execute(testRun.Tests, null, cancellationToken);

            var compiledResults = executionSummaryCompiler.CompileToSummaries(rawExecutionResults, cancellationToken);

            await testResultPresenter.PresentResults(compiledResults, timeStamp, runSettings, cancellationToken);

            var exceptions = compiledResults.SelectMany(x => x.CompiledResults.Select(j => j.Exception)).Where(x => x is not null);
            return rawExecutionResults.Select(x => x.IsSuccess).All(x => x)
                ? SailfishValidity.CreateValidResult()
                : SailfishValidity.CreateInvalidResult(exceptions!);
        }

        Log.Logger.Error("{NumErrors} errors encountered while discovering tests", testRun.Errors.Count);
        foreach (var (reason, names) in testRun.Errors)
        {
            Log.Logger.Error("{Reason}", reason);
            foreach (var testName in names) Log.Logger.Error("--- {TestName}", testName);
        }

        return SailfishValidity.CreateInvalidResult(Enumerable.Empty<Exception>());
    }

    private TestValidationResult CollectTests(string[] testNames, params Type[] locationTypes)
    {
        var perfTests = testCollector.CollectTestTypes(locationTypes);
        return testFilter.FilterAndValidate(perfTests, testNames);
    }
}