using System;
using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Statistics;

public class TestResultCompiler : ITestResultCompiler
{
    private readonly IStatisticsCompiler statsCompiler;

    public readonly bool AsCsv = true;

    public TestResultCompiler(IStatisticsCompiler statsCompiler)
    {
        this.statsCompiler = statsCompiler;
    }

    public List<CompiledResultContainer> CompileResults(Dictionary<Type, List<TestExecutionResult>> results)
    {
        var resultContainers = new List<CompiledResultContainer>();
        foreach (var (type, resultList) in results)
        {
            // a testExecutionResult is the result from a single set of variables on a single method on a single class - it comes from a TestExecutionContainer
            // We Might have some cases that just don't work (or surpass a timeout we impose) - these are considered failures

            var compiledResults = new List<CompiledResult>();
            var exceptions = new List<Exception>();

            foreach (var resultListElement in resultList)
            {
                var exception = resultListElement.Exception;
                if (exception is not null)
                {
                    var compiledResult = new CompiledResult(
                        resultListElement.IsSuccess ? resultListElement.TestInstanceContainer.DisplayName : "",
                        resultListElement.IsSuccess ? resultListElement.TestInstanceContainer.GroupingId : "",
                        null!);

                    compiledResult.Exception = exception;
                    exceptions.Add(exception);
                }
                else
                {
                    var stats = ComputeStatistics(resultListElement);
                    var compiledResult = new CompiledResult(
                        resultListElement.TestInstanceContainer.DisplayName,
                        resultListElement.TestInstanceContainer.GroupingId,
                        stats);
                    compiledResults.Add(compiledResult);
                }
            }

            var settings = type.RetrieveExecutionTestSettings();
            var container = new CompiledResultContainer(exceptions, type, compiledResults, settings);
            resultContainers.Add(container);
        }

        return resultContainers;
    }

    private TestCaseStatistics ComputeStatistics(TestExecutionResult result)
    {
        return statsCompiler.Compile(result.TestInstanceContainer.DisplayName, result.PerformanceTimerResults);
    }
}