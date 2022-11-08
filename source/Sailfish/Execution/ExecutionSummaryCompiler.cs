using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sailfish.Contracts.Public;
using Sailfish.Exceptions;
using Sailfish.Statistics;

namespace Sailfish.Execution;

internal class ExecutionSummaryCompiler : IExecutionSummaryCompiler
{
    private readonly IStatisticsCompiler statsCompiler;

    public ExecutionSummaryCompiler(IStatisticsCompiler statsCompiler)
    {
        this.statsCompiler = statsCompiler;
    }

    public List<ExecutionSummary> CompileToSummaries(List<RawExecutionResult> rawExecutionResults, CancellationToken cancellationToken)
    {
        return rawExecutionResults.Select(rawExecutionResult => IterateExecutionResults(rawExecutionResult, cancellationToken)).ToList();
    }

    private ExecutionSummary IterateExecutionResults(RawExecutionResult rawExecutionResult, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compiledResults = new List<CompiledResult>();

        if (rawExecutionResult.ExecutionResults != null)
        {
            foreach (var testExecutionResult in rawExecutionResult.ExecutionResults)
            {
                CompileTestResult(testExecutionResult, compiledResults);
            }

            return new ExecutionSummary(rawExecutionResult.TestType, compiledResults);
        }

        if (rawExecutionResult.Exception is null) return new ExecutionSummary(rawExecutionResult.TestType, new List<CompiledResult>() { });
        var compiledResult = new CompiledResult(rawExecutionResult.Exception);
        return new ExecutionSummary(rawExecutionResult.TestType, new List<CompiledResult>() { compiledResult });
    }

    private void CompileTestResult(TestExecutionResult testExecutionResult, ICollection<CompiledResult> compiledResults)
    {
        if (testExecutionResult.IsSuccess)
        {
            if (testExecutionResult.IsSuccess && !testExecutionResult.PerformanceTimerResults.IsValid)
            {
                var message = $"Somehow the test exception was successful, but the performance " +
                              $"timer was invalid for test: {testExecutionResult.TestInstanceContainer.DisplayName}";
                throw new SailfishException(message);
            }

            var descriptiveStatistics = ComputeStatistics(testExecutionResult);
            var compiledResult = new CompiledResult(
                testExecutionResult.TestInstanceContainer.DisplayName,
                testExecutionResult.TestInstanceContainer.GroupingId,
                descriptiveStatistics);

            if (testExecutionResult.Exception is not null)
            {
                compiledResult.Exception = testExecutionResult.Exception;
            }

            compiledResults.Add(compiledResult);
        }
        else
        {
            if (testExecutionResult.Exception is null) return;
            var compiledResult = new CompiledResult(testExecutionResult.Exception);
            compiledResults.Add(compiledResult);
            // if we have a failure, but no exception -- we have no information to report
        }
    }

    private DescriptiveStatisticsResult ComputeStatistics(TestExecutionResult result)
    {
        return statsCompiler.Compile(result.TestInstanceContainer.DisplayName, result.PerformanceTimerResults);
    }
}