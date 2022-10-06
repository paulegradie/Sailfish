using System.Collections.Generic;
using Sailfish.Contracts.Public;
using Sailfish.Exceptions;
using Sailfish.Execution;

namespace Sailfish.Statistics;

internal class ExecutionSummaryCompiler : IExecutionSummaryCompiler
{
    private readonly IStatisticsCompiler statsCompiler;

    public ExecutionSummaryCompiler(IStatisticsCompiler statsCompiler)
    {
        this.statsCompiler = statsCompiler;
    }

    /// <summary>
    /// This is effectively a mapper, that maps 
    /// </summary>
    /// <param name="results"></param>
    /// <param name="rawExecutionResults"></param>
    /// <returns></returns>
    public List<ExecutionSummary> CompileToSummaries(List<RawExecutionResult> rawExecutionResults)
    {
        var executionSummaries = new List<ExecutionSummary>();
        foreach (var rawExecutionResult in rawExecutionResults)
        {
            var summary = IterateExecutionResults(rawExecutionResult);

            executionSummaries.Add(summary);
        }

        return executionSummaries;
    }

    private ExecutionSummary IterateExecutionResults(RawExecutionResult rawExecutionResult)
    {
        var compiledResults = new List<CompiledResult>();

        if (rawExecutionResult.ExecutionResults != null)
        {
            foreach (var testExecutionResult in rawExecutionResult.ExecutionResults)
            {
                CompileTestResult(testExecutionResult, compiledResults);
            }

            return new ExecutionSummary(rawExecutionResult.TestType, compiledResults);
        }

        else if (rawExecutionResult.Exception is not null)
        {
            var compiledResult = new CompiledResult(rawExecutionResult.Exception);
            return new ExecutionSummary(rawExecutionResult.TestType, new List<CompiledResult>() { compiledResult });
        }
        else
        {
            return new ExecutionSummary(rawExecutionResult.TestType, new List<CompiledResult>() { });
        }
    }

    private void CompileTestResult(TestExecutionResult testExecutionResult, List<CompiledResult> compiledResults)
    {
        if (testExecutionResult.IsSuccess)
        {
            if (testExecutionResult.IsSuccess && !testExecutionResult.PerformanceTimerResults.IsValid) throw new SailfishException($"Somehow test exception was successful, but the performance timer was invalid for test: {testExecutionResult.TestInstanceContainer.DisplayName}");
            var descriptiveStatistics = ComputeStatistics(testExecutionResult);
            var compiledResult = new CompiledResult(testExecutionResult.TestInstanceContainer.DisplayName, testExecutionResult.TestInstanceContainer.GroupingId, descriptiveStatistics);
            if (testExecutionResult.Exception is not null)
            {
                compiledResult.Exception = testExecutionResult.Exception;
            }

            compiledResults.Add(compiledResult);
        }
        else
        {
            if (testExecutionResult.Exception is not null)
            {
                var compiledResult = new CompiledResult(testExecutionResult.Exception);
                compiledResults.Add(compiledResult);
            }
            // if we have a failure, but no exception -- we have no information to report
        }
    }

    private DescriptiveStatisticsResult ComputeStatistics(TestExecutionResult result)
    {
        return statsCompiler.Compile(result.TestInstanceContainer.DisplayName, result.PerformanceTimerResults);
    }
}