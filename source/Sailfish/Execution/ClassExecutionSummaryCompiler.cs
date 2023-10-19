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

    public IEnumerable<IClassExecutionSummary> CompileToSummaries(IEnumerable<TestClassResultGroup> rawExecutionResults, CancellationToken cancellationToken)
    {
        return rawExecutionResults.Select(rawExecutionResult => IterateExecutionResults(rawExecutionResult, cancellationToken)).ToList();
    }

    private IClassExecutionSummary IterateExecutionResults(TestClassResultGroup testClassResultGroup, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compiledResults = new List<ICompiledTestCaseResult>();
        foreach (var testClassExecutionResult in testClassResultGroup.ExecutionResults)
        {
            var compiledResult = CompileTestResult(testClassExecutionResult, compiledResults);
            compiledResults.Add(compiledResult);
        }

        return new ClassExecutionSummary(testClassResultGroup.TestClass, compiledResults);
    }

    private CompiledTestCaseResult CompileTestResult(TestCaseExecutionResult testCaseExecutionResult, ICollection<ICompiledTestCaseResult> compiledResults)
    {
        if (!testCaseExecutionResult.IsSuccess)
        {
            return new CompiledTestCaseResult(
                testCaseExecutionResult.TestInstanceContainer?.TestCaseId,
                testCaseExecutionResult.TestInstanceContainer?.GroupingId,
                testCaseExecutionResult.Exception ?? new SailfishException("Encountered test failure but could not find an exception")
            );
        }

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
        if (testCaseExecutionResult.Exception is not null)
        {
            throw testCaseExecutionResult.Exception;
        }

        return compiledResult;
    }

    private PerformanceRunResult ComputeStatistics(TestCaseExecutionResult result, IExecutionSettings executionSettings)
    {
        return statsCompiler.Compile(result.TestInstanceContainer?.TestCaseId!, result.PerformanceTimerResults!, executionSettings);
    }
}