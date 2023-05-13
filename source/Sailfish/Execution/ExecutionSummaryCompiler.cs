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

    public IEnumerable<IExecutionSummary> CompileToSummaries(IEnumerable<RawExecutionResult> rawExecutionResults, CancellationToken cancellationToken)
    {
        return rawExecutionResults.Select(rawExecutionResult => IterateExecutionResults(rawExecutionResult, cancellationToken)).ToList();
    }

    private IExecutionSummary IterateExecutionResults(RawExecutionResult rawExecutionResult, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compiledResults = new List<ICompiledResult>();

        if (rawExecutionResult.ExecutionResults != null)
        {
            foreach (var testExecutionResult in rawExecutionResult.ExecutionResults)
            {
                CompileTestResult(testExecutionResult, compiledResults);
            }

            return new ExecutionSummary(rawExecutionResult.TestType, compiledResults);
        }

        if (!rawExecutionResult.Exceptions.Any()) return new ExecutionSummary(rawExecutionResult.TestType, new List<ICompiledResult>() { });
        var compiledResult = new CompiledResult(rawExecutionResult.Exceptions);
        return new ExecutionSummary(rawExecutionResult.TestType, new List<ICompiledResult>() { compiledResult });
    }

    private void CompileTestResult(TestExecutionResult testExecutionResult, ICollection<ICompiledResult> compiledResults)
    {
        if (testExecutionResult.IsSuccess)
        {
            if (testExecutionResult is { PerformanceTimerResults.IsValid: false, IsSuccess: true })
            {
                var message = $"Somehow the test exception was successful, but the performance " +
                              $"timer was invalid for test: {testExecutionResult.TestInstanceContainer?.TestCaseId.DisplayName}";
                throw new SailfishException(message);
            }

            var descriptiveStatistics = ComputeStatistics(testExecutionResult);
            var compiledResult = new CompiledResult(
                testExecutionResult.TestInstanceContainer?.TestCaseId!,
                testExecutionResult.TestInstanceContainer?.GroupingId!,
                descriptiveStatistics);

            if (testExecutionResult.Exception is not null)
            {
                compiledResult.Exceptions.Add(testExecutionResult.Exception);
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
        return statsCompiler.Compile(result.TestInstanceContainer?.TestCaseId!, result.PerformanceTimerResults!);
    }
}