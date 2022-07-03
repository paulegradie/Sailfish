using System;
using System.Collections.Generic;
using Sailfish.Execution;
using Sailfish.ExtensionMethods;

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
    /// <returns></returns>
    public List<ExecutionSummary> CompileToSummaries(List<RawExecutionResult> rawExecutionResults)
    {
        var executionSummaries = new List<ExecutionSummary>();
        foreach (var rawExecutionResult in rawExecutionResults)
        {
            // a testExecutionResult is the result from a single set of variables on a single method on a single class - it comes from a TestExecutionContainer
            // We Might have some cases that just don't work (or surpass a timeout we impose) - these are considered failures

            var compiledResults = new List<CompiledResult>();
            var exceptions = new List<Exception>();

            foreach (var testExecutionResult in rawExecutionResult.ExecutionResults)
            {
                if (testExecutionResult.IsSuccess)
                {
                    var stats = ComputeStatistics(testExecutionResult);
                    var compiledResult = CompiledResult.CreateSuccessfulCompiledResult(testExecutionResult.TestInstanceContainer.DisplayName, testExecutionResult.TestInstanceContainer.GroupingId, stats);
                    if (testExecutionResult.Exception is not null)
                    {
                        compiledResult.Exception = testExecutionResult.Exception
                    }
                }
                
                var exception = testExecutionResult.Exception;
                if (exception is not null)
                {
                    var compiledResult = new CompiledResult(
                        testExecutionResult.IsSuccess ? testExecutionResult.TestInstanceContainer.DisplayName : "",
                        testExecutionResult.IsSuccess ? testExecutionResult.TestInstanceContainer.GroupingId : "",
                        null!);

                    compiledResult.Exception = exception;
                    exceptions.Add(exception);
                }
                else
                {
                    var stats = ComputeStatistics(testExecutionResult);
                    var compiledResult = new CompiledResult(
                        testExecutionResult.TestInstanceContainer.DisplayName,
                        testExecutionResult.TestInstanceContainer.GroupingId,
                        stats);
                    compiledResults.Add(compiledResult);
                }
            }

            var settings = type.RetrieveExecutionTestSettings();
            var container = new ExecutionSummary(exceptions, type, compiledResults, settings);
            executionSummaries.Add(container);
        }

        return executionSummaries;
    }

    private TestCaseStatistics ComputeStatistics(TestExecutionResult result)
    {
        return statsCompiler.Compile(result.TestInstanceContainer.DisplayName, result.PerformanceTimerResults);
    }
}