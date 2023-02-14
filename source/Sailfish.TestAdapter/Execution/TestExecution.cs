using System;
using System.Collections.Generic;
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

            var firstTestCase = testCaseGroup.First();
            var testTypeTrait = firstTestCase.Traits.Single(trait => trait.Name == TestCaseItemCreator.TestTypeFullName);
            var testTypeFullName = testTypeTrait.Value;
            var assembly = LoadAssemblyFromDll(firstTestCase.Source);
            var testType = assembly.GetType(testTypeFullName, true, true);
            if (testType is null)
            {
                frameworkHandle?.SendMessage(TestMessageLevel.Error, $"Unable to find the following testType: {testTypeFullName}");
                continue;
            }

            var availableVariableSections = testCases.Select(x => x.Traits.Single(y => y.Name == TestCaseItemCreator.FormedVariableSection).Value).Distinct();

            bool PropertyFilter(PropertySet currentPropertySet)
            {
                var currentVariableSection = currentPropertySet.FormTestCaseVariableSection();
                return availableVariableSections.Contains(currentVariableSection);
            }

            var availableMethods = testCases.Select(x => x.Traits.Single(y => y.Name == TestCaseItemCreator.MethodName).Value).Distinct();

            bool MethodFilter(MethodInfo currentMethodInfo)
            {
                var currentMethod = currentMethodInfo.Name;
                return availableMethods.Contains(currentMethod);
            }

            // list of methods with their many variable combos. Each element is a container, which represents a SailfishMethod
            var providerForCurrentTestCases = TestInstanceContainerCreator.CreateTestContainerInstanceProviders(testType, PropertyFilter, MethodFilter);

            var totalTestProviderCount = providerForCurrentTestCases.Count - 1;
            for (var i = 0; i < providerForCurrentTestCases.Count; i++)
            {
                var provider = providerForCurrentTestCases[i];
                var results = engine.ActivateContainer(
                        i,
                        totalTestProviderCount,
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

    private static Action<TestInstanceContainer> PreTestResultCallback(ITestExecutionRecorder? logger, IGrouping<string, TestCase> testCaseGroup)
    {
        return container =>
        {
            var currentTestCase = GetTestCaseFromTestCaseGroupMatchingCurrentContainer(container, testCaseGroup);
            logger?.RecordStart(currentTestCase);
        };
    }

    private static TestCase GetTestCaseFromTestCaseGroupMatchingCurrentContainer(TestInstanceContainer container, IGrouping<string, TestCase> testCaseGroup)
    {
        return testCaseGroup.Single(x => string.Equals(x.DisplayName, container.TestCaseId.DisplayName, StringComparison.InvariantCultureIgnoreCase));
    }

    private static Action<TestExecutionResult, TestInstanceContainer> PostTestResultCallback(IGrouping<string, TestCase> testCaseGroups, ITestExecutionRecorder? logger,
        CancellationToken cancellationToken)
    {
        return (result, container) =>
        {
            var currentTestCase = GetTestCaseFromTestCaseGroupMatchingCurrentContainer(container, testCaseGroups);

            var rawResult = result.Exception is null
                ? new RawExecutionResult(result.TestInstanceContainer.Type, new List<TestExecutionResult>() { result })
                : new RawExecutionResult(result.TestInstanceContainer.Type, result.Exception);

            var compiledResult = SummaryCompiler.CompileToSummaries(new List<RawExecutionResult>() { rawResult }, cancellationToken).ToList();
            var medianTestRuntime = compiledResult.Single().CompiledResults.Single().DescriptiveStatisticsResult?.Median ??
                                    throw new SailfishException("Error computing compiled results");

            var testResult = new TestResult(currentTestCase);

            if (result.Exception is not null)
            {
                testResult.ErrorMessage = result.Exception.Message;
                testResult.ErrorStackTrace = result.Exception.StackTrace;
            }

            testResult.Outcome = result.StatusCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;
            testResult.DisplayName = currentTestCase.DisplayName;

            testResult.StartTime = result.PerformanceTimerResults.GlobalStart;
            testResult.EndTime = result.PerformanceTimerResults.GlobalStop;
            testResult.Duration = TimeSpan.FromMilliseconds(medianTestRuntime);

            testResult.ErrorMessage = result.Exception?.Message;

            var outputs = ConsoleWriter(logger).Present(compiledResult);
            testResult.Messages.Add(new TestResultMessage("Test Result", outputs));

            LogTestResults(result, logger);

            logger?.RecordResult(testResult);
            logger?.RecordEnd(currentTestCase, testResult.Outcome);
        };
    }
}