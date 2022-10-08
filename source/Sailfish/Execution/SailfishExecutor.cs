using System;
using System.Linq;
using System.Threading.Tasks;
using Sailfish.Presentation;
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

    public async Task<SailfishValidity> Run(RunSettings runSettings)
    {
        return await Run(
            testNames: runSettings.TestNames,
            runSettings: runSettings,
            testLocationTypes: runSettings.TestLocationTypes
        );
    }

    public async Task<SailfishValidity> Run(
        string[] testNames,
        RunSettings runSettings,
        params Type[] testLocationTypes)
    {
        var testRun = CollectTests(testNames, testLocationTypes);
        if (testRun.IsValid)
        {
            var timeStamp = runSettings.TimeStamp ?? DateTime.Now.ToLocalTime();

            var rawExecutionResults = await sailFishTestExecutor.Execute(testRun.Tests);

            var compiledResults = executionSummaryCompiler.CompileToSummaries(rawExecutionResults);

            await testResultPresenter.PresentResults(compiledResults, timeStamp, runSettings);

            return rawExecutionResults.Select(x => x.IsSuccess).All(x => x)
                ? SailfishValidity.CreateValidResult()
                : SailfishValidity.CreateInvalidResult();
        }

        Console.WriteLine("\r----------- Error ------------\r");
        foreach (var (reason, names) in testRun.Errors)
        {
            Console.WriteLine(reason);
            foreach (var testName in names) Console.WriteLine($"--- {testName}");
        }

        return SailfishValidity.CreateInvalidResult();
    }

    public TestValidationResult CollectTests(string[] testNames, params Type[] locationTypes)
    {
        var perfTests = testCollector.CollectTestTypes(locationTypes);
        return testFilter.FilterAndValidate(perfTests, testNames);
    }
}

public class SailfishValidity
{
    private SailfishValidity(bool isValid)
    {
        IsValid = isValid;
    }

    public bool IsValid { get; set; }

    public static SailfishValidity CreateValidResult()
    {
        return new SailfishValidity(true);
    }

    public static SailfishValidity CreateInvalidResult()
    {
        return new SailfishValidity(false);
    }
}