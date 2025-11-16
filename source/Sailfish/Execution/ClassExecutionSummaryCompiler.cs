using System.Collections.Generic;
using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics;
using Sailfish.Contracts.Public.Models;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

internal interface IClassExecutionSummaryCompiler
{
    IEnumerable<IClassExecutionSummary> CompileToSummaries(IEnumerable<TestClassResultGroup> results);
}

internal class ClassExecutionSummaryCompiler : IClassExecutionSummaryCompiler
{
    private readonly IRunSettings _runSettings;
    private readonly IStatisticsCompiler _statsCompiler;

    public ClassExecutionSummaryCompiler(IStatisticsCompiler statsCompiler, IRunSettings runSettings)
    {
        _runSettings = runSettings;
        _statsCompiler = statsCompiler;
    }

    public IEnumerable<IClassExecutionSummary> CompileToSummaries(IEnumerable<TestClassResultGroup> rawExecutionResults)
    {
        return rawExecutionResults.Select(IterateExecutionResults).ToList();
    }

    private IClassExecutionSummary IterateExecutionResults(TestClassResultGroup testClassResultGroup)
    {
        var compiledResults = new List<ICompiledTestCaseResult>();
        foreach (var testClassExecutionResult in testClassResultGroup.ExecutionResults)
        {
            var compiledResult = CompileTestResult(testClassExecutionResult);
            compiledResults.Add(compiledResult);
        }

        var executionSettings = testClassResultGroup.TestClass.RetrieveExecutionTestSettings(
            _runSettings.SampleSizeOverride,
            _runSettings.NumWarmupIterationsOverride,
            _runSettings.GlobalUseAdaptiveSampling,
            _runSettings.GlobalTargetCoefficientOfVariation,
            _runSettings.GlobalMaximumSampleSize);
        return new ClassExecutionSummary(
            testClassResultGroup.TestClass,
            executionSettings,
            compiledResults);
    }

    private CompiledTestCaseResult CompileTestResult(TestCaseExecutionResult testCaseExecutionResult)
    {
        if (!testCaseExecutionResult.IsSuccess)
            return new CompiledTestCaseResult(
                testCaseExecutionResult.TestInstanceContainer?.TestCaseId!,
                testCaseExecutionResult.TestInstanceContainer?.GroupingId,
                testCaseExecutionResult.Exception ?? new SailfishException("Encountered test failure but could not find an exception")
            );

        if (testCaseExecutionResult is { PerformanceTimerResults.IsValid: false, IsSuccess: true })
        {
            var message = $"Somehow " +
                          $"the test exception was successful, but the performance " +
                          $"timer was invalid for test: {testCaseExecutionResult.TestInstanceContainer?.TestCaseId.DisplayName}";
            throw new SailfishException(message);
        }

        var descriptiveStatistics = ComputeStatistics(testCaseExecutionResult, testCaseExecutionResult.ExecutionSettings ?? new ExecutionSettings());
        var compiledResult = new CompiledTestCaseResult(
            testCaseExecutionResult.TestInstanceContainer?.TestCaseId!,
            testCaseExecutionResult.TestInstanceContainer?.GroupingId!,
            descriptiveStatistics);

        // should never be the case...
        if (testCaseExecutionResult.Exception is not null) throw testCaseExecutionResult.Exception;

        return compiledResult;
    }

    private PerformanceRunResult ComputeStatistics(TestCaseExecutionResult result, IExecutionSettings executionSettings)
    {
        return _statsCompiler.Compile(result.TestInstanceContainer?.TestCaseId!, result.PerformanceTimerResults!, executionSettings);
    }
}