using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Accord.Collections;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Statistics;
using Sailfish.TestAdapter.Discovery;

namespace Sailfish.TestAdapter.Execution;

class TestCaseGroupedByMethod
{
    private readonly TestCase[] testCases;

    public TestCaseGroupedByMethod(TestCase[] testCases)
    {
        this.testCases = testCases;
    }
}

internal static class TestExecution
{
    private static readonly ExecutionSummaryCompiler SummaryCompiler = new(new StatisticsCompiler());
    private static readonly Func<ITestExecutionRecorder?, ConsoleWriter> ConsoleWriter = handle => new(new PresentationStringConstructor(), handle);

    public static void ExecuteTests(List<TestCase> testCases, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken)
    {
        if (testCases.Count == 0)
        {
            frameworkHandle?.SendMessage(TestMessageLevel.Informational, "No Sailfish tests were discovered");
            return;
        }

        var engine = new SailfishExecutionEngine(new TestCaseIterator());
        var rawResults = new List<RawExecutionResult>();

        var testCaseGroups = testCases
            .GroupBy(testCase => testCase.Traits.Single(x => x.Name == TestCaseItemCreator.MethodName).Value);

        foreach (var testCaseGroup in testCaseGroups)
        {
            var groupResults = new List<TestExecutionResult>();
            Type? testType = null;
            foreach (var testCase in testCaseGroup)
            {
                var testTypeTrait = testCase.Traits.Single(trait => trait.Name == TestCaseItemCreator.TestTypeFullName);
                var testTypeFullName = testTypeTrait.Value;

                var assembly = LoadAssemblyFromDll(testCase.Source);

                var nextTestType = assembly.GetType(testTypeFullName, true, true);

                if (nextTestType is null)
                {
                    frameworkHandle?.SendMessage(TestMessageLevel.Error, $"Unable to find the following testType: {testTypeFullName}");
                    continue;
                }

                if (testType is not null && testType.FullName != nextTestType.FullName)
                {
                    throw new Exception($"The test types in the group should be matching: current: {testType.FullName} - next: {nextTestType.FullName}");
                }

                testType = nextTestType;

                var typeResolve = assembly.GetTypeResolverOrNull();
                var testContainerCreator = new TestInstanceContainerCreator(
                    typeResolve,
                    new PropertySetGenerator(
                        new ParameterCombinator(),
                        new IterationVariableRetriever()));

                var formedVariableSection = testCase.Traits.Single(t => t.Name == TestCaseItemCreator.FormedVariableSection).Value;
                var methodName = testCase.Traits.Single(t => t.Name == TestCaseItemCreator.MethodName).Value;

                bool PropertyFilter(PropertySet currentPropertySet)
                {
                    var currentVariableSection = currentPropertySet.FormTestCaseVariableSection();
                    return currentVariableSection.Equals(formedVariableSection, StringComparison.InvariantCultureIgnoreCase);
                }

                bool MethodFilter(MethodInfo currentMethodInfo)
                {
                    var currentMethod = currentMethodInfo.Name;
                    return currentMethod.Equals(methodName, StringComparison.InvariantCultureIgnoreCase);
                }

                var testInstanceContainerProviderToMatchTheCurrentTestCases = testContainerCreator.CreateTestContainerInstanceProviders(testType, PropertyFilter, MethodFilter);
                var testInstanceContainerProviderToMatchTheCurrentTestCase = testInstanceContainerProviderToMatchTheCurrentTestCases.Single();
                frameworkHandle?.RecordStart(testCase);
                var results = engine.ActivateContainer(
                        0,
                        1,
                        testInstanceContainerProviderToMatchTheCurrentTestCase,
                        TestResultCallback(testCase, frameworkHandle, cancellationToken),
                        cancellationToken)
                    .GetAwaiter().GetResult();
                groupResults.AddRange(results);
            }

            if (testType is null) throw new Exception($"Test type was null in TestExecution.cs: line 105");
            rawResults.Add(new RawExecutionResult(testType, groupResults));
        }

        var compiledResults = SummaryCompiler.CompileToSummaries(rawResults, CancellationToken.None);
        ConsoleWriter(frameworkHandle).Present(compiledResults);
    }

    private static Assembly LoadAssemblyFromDll(string dllPath)
    {
        var assembly = Assembly.LoadFile(dllPath);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?
        return assembly;
    }

    private static void LogTestResults(TestExecutionResult result, IMessageLogger? logger)
    {
        foreach (var perf in result.PerformanceTimerResults.MethodIterationPerformances)
        {
            logger?.SendMessage(TestMessageLevel.Informational, $"Time: {perf.Duration.ToString()} ms");
        }
    }

    private static Action<TestExecutionResult> TestResultCallback(TestCase testCase, ITestExecutionRecorder? logger, CancellationToken cancellationToken)
    {
        return (TestExecutionResult result) =>
        {
            var testResult = new TestResult(testCase);

            if (result.Exception is not null)
            {
                testResult.ErrorMessage = result.Exception.Message;
                testResult.ErrorStackTrace = result.Exception.StackTrace;
            }

            testResult.Outcome = result.StatusCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;
            testResult.DisplayName = testCase.DisplayName;

            testResult.StartTime = result.PerformanceTimerResults.GlobalStart;
            testResult.EndTime = result.PerformanceTimerResults.GlobalStop;
            testResult.Duration = result.PerformanceTimerResults.GlobalDuration;

            testResult.ErrorMessage = result.Exception?.Message;

            var rawResult = result.Exception is null
                ? new RawExecutionResult(result.TestInstanceContainer.Type, new List<TestExecutionResult>() { result })
                : new RawExecutionResult(result.TestInstanceContainer.Type, result.Exception);

            var compiledResult = SummaryCompiler.CompileToSummaries(new List<RawExecutionResult>() { rawResult }, cancellationToken);
            var outputs = ConsoleWriter(logger).Present(compiledResult);
            testResult.Messages.Add(new TestResultMessage("Test Result", outputs));

            LogTestResults(result, logger);

            logger?.RecordResult(testResult);
            logger?.RecordEnd(testCase, testResult.Outcome);
        };
    }
}