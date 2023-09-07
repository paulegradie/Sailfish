using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sailfish.Contracts.Public;
using Sailfish.Exceptions;
using Sailfish.Statistics;

namespace Sailfish.Execution;

internal class ClassExecutionSummaryCompiler : IClassExecutionSummaryCompiler
{
    private readonly IStatisticsCompiler statsCompiler;

    public ClassExecutionSummaryCompiler(IStatisticsCompiler statsCompiler)
    {
        this.statsCompiler = statsCompiler;
    }

    public IEnumerable<IClassExecutionSummary> CompileToSummaries(IEnumerable<RawExecutionResult> rawExecutionResults, CancellationToken cancellationToken)
    {
        return rawExecutionResults.Select(rawExecutionResult => IterateExecutionResults(rawExecutionResult, cancellationToken)).ToList();
    }

    private IClassExecutionSummary IterateExecutionResults(RawExecutionResult rawExecutionResult, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compiledResults = new List<ICompiledTestCaseResult>();

        if (rawExecutionResult.ExecutionResults != null)
        {
            foreach (var testExecutionResult in rawExecutionResult.ExecutionResults)
            {
                CompileTestResult(testExecutionResult, compiledResults);
            }

            return new ClassExecutionSummary(rawExecutionResult.TestType, compiledResults);
        }

        if (!rawExecutionResult.Exceptions.Any()) return new ClassExecutionSummary(rawExecutionResult.TestType, new List<ICompiledTestCaseResult>() { });
        var compiledResult = new CompiledTestCaseResult(rawExecutionResult.Exceptions);
        return new ClassExecutionSummary(rawExecutionResult.TestType, new List<ICompiledTestCaseResult>() { compiledResult });
    }

    private void CompileTestResult(TestExecutionResult testExecutionResult, ICollection<ICompiledTestCaseResult> compiledResults)
    {
        if (testExecutionResult.IsSuccess)
        {
            if (testExecutionResult is { PerformanceTimerResults.IsValid: false, IsSuccess: true })
            {
                var message = $"Somehow " +
                              $"the test exception was successful, but the performance " +
                              $"timer was invalid for test: {testExecutionResult.TestInstanceContainer?.TestCaseId.DisplayName}";
                throw new SailfishException(message);
            }

            var descriptiveStatistics = ComputeStatistics(testExecutionResult, testExecutionResult.ExecutionSettings ?? new ExecutionSettings());
            var compiledResult = new CompiledTestCaseResult(
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
            var compiledResult = new CompiledTestCaseResult(testExecutionResult.Exception);
            compiledResults.Add(compiledResult);
            // if we have a failure, but no exception -- we have no information to report
        }
    }

    private PerformanceRunResult ComputeStatistics(TestExecutionResult result, IExecutionSettings executionSettings)
    {
        return statsCompiler.Compile(result.TestInstanceContainer?.TestCaseId!, result.PerformanceTimerResults!, executionSettings);
    }
}