using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Statistics;
using Sailfish.TestAdapter.Discovery;

namespace Sailfish.TestAdapter.Execution;

internal static class TestExecution
{
    private static readonly ExecutionSummaryCompiler SummaryCompiler = new(new StatisticsCompiler());

    private static readonly Func<ITestExecutionRecorder?, ConsoleWriter> ConsoleWriter = handle =>
        new(new MarkdownTableConverter(), handle);

    private static readonly TestInstanceContainerCreator TestInstanceContainerCreator = new(
        new TypeResolutionUtility(),
        new PropertySetGenerator(
            new ParameterCombinator(),
            new IterationVariableRetriever()));

    public static void ExecuteTests(List<TestCase> testCases, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken)
    {
        if (testCases.Count == 0)
        {
            frameworkHandle?.SendMessage(TestMessageLevel.Informational, "No Sailfish tests were discovered");
            return;
        }

        var adapterRunSettings = RunSettings.CreateTestAdapterSettings();

        var engine = new SailfishExecutionEngine(new TestCaseIterator(), adapterRunSettings);
        var rawResults = new List<RawExecutionResult>();

        var testCaseGroups = testCases
            .GroupBy(
                testCase =>
                    testCase.Traits.Single(x => x.Name == TestCaseItemCreator.TestTypeFullName).Value);

        foreach (var testCaseGroup in testCaseGroups)
        {
            var groupResults = new List<TestExecutionResult>();
            Type? testType = null;

            var firstTestCase = testCaseGroup.First();
            var testTypeTrait = firstTestCase.Traits.Single(trait => trait.Name == TestCaseItemCreator.TestTypeFullName);
            var testTypeFullName = testTypeTrait.Value;
            var assembly = LoadAssemblyFromDll(firstTestCase.Source);
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

            // list of methods with their many variable combos. Each element is a container, which represents a SailfishMethod
            var providerForCurrentTestCases = TestInstanceContainerCreator.CreateTestContainerInstanceProviders(testType); //, PropertyFilter, MethodFilter);

            for (var i = 0; i < providerForCurrentTestCases.Count; i++)
            {
                var provider = providerForCurrentTestCases[i];
                var results = engine.ActivateContainer(
                        i,
                        providerForCurrentTestCases.Count,
                        provider,
                        PreTestResultCallback(frameworkHandle, testCaseGroup),
                        PostTestResultCallback(testCaseGroup, frameworkHandle, cancellationToken),
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

    private static Action<TestInstanceContainerProvider> PreTestResultCallback(ITestExecutionRecorder? logger, IGrouping<string, TestCase> testCaseGroup)
    {
        return provider =>
        {
            var currentTestCase = testCaseGroup.Single(x => x.DisplayName == provider.Test.FullName);
            logger?.RecordStart(currentTestCase);
        };
    }

    private static Action<TestExecutionResult> PostTestResultCallback(IGrouping<string, TestCase> testCaseGroups, ITestExecutionRecorder? logger, CancellationToken cancellationToken)
    {
        return result =>
        {
            var testCase = testCaseGroups.Single(x => x.FullyQualifiedName == result.TestInstanceContainer.Type.FullName);
            
            var rawResult = result.Exception is null
                ? new RawExecutionResult(result.TestInstanceContainer.Type, new List<TestExecutionResult>() { result })
                : new RawExecutionResult(result.TestInstanceContainer.Type, result.Exception);

            var compiledResult = SummaryCompiler.CompileToSummaries(new List<RawExecutionResult>() { rawResult }, cancellationToken).ToList();
            var medianTestRuntime = compiledResult.Single().CompiledResults.Single().DescriptiveStatisticsResult?.Median ??
                                    throw new SailfishException("Error computing compiled results");

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
            testResult.Duration = TimeSpan.FromMilliseconds(medianTestRuntime);

            testResult.ErrorMessage = result.Exception?.Message;

            var outputs = ConsoleWriter(logger).Present(compiledResult);
            testResult.Messages.Add(new TestResultMessage("Test Result", outputs));

            LogTestResults(result, logger);

            logger?.RecordResult(testResult);
            logger?.RecordEnd(testCase, testResult.Outcome);
        };
    }
}