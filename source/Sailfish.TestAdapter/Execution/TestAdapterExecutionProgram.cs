using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter.Execution;

internal class TestAdapterExecutionProgram : ITestAdapterExecutionProgram
{
    private readonly ITestInstanceContainerCreator testInstanceContainerCreator;
    private readonly IConsoleWriterFactory consoleWriterFactory;
    private readonly IExecutionSummaryCompiler executionSummaryCompiler;
    private readonly ISailfishExecutionEngine engine;
    private const string MemoryCacheName = "GlobalStateMemoryCache";

    public TestAdapterExecutionProgram(
        ITestInstanceContainerCreator testInstanceContainerCreator,
        IConsoleWriterFactory consoleWriterFactory,
        IExecutionSummaryCompiler executionSummaryCompiler,
        ISailfishExecutionEngine engine)
    {
        this.testInstanceContainerCreator = testInstanceContainerCreator;
        this.consoleWriterFactory = consoleWriterFactory;
        this.executionSummaryCompiler = executionSummaryCompiler;
        this.engine = engine;
    }

    public void Run(List<TestCase> testCases, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken)
    {
        if (testCases.Count == 0)
        {
            frameworkHandle?.SendMessage(TestMessageLevel.Informational, "No Sailfish tests were discovered");
            return;
        }

        var rawResults = new List<RawExecutionResult>();
        var testCaseGroups = testCases.GroupBy(testCase => testCase.GetPropertyHelper(SailfishTestTypeFullNameDefinition.SailfishTestTypeFullNameDefinitionProperty));

        foreach (var testCaseGroup in testCaseGroups)
        {
            var groupResults = new List<TestExecutionResult>();

            var firstTestCase = testCaseGroup.First();
            var testTypeFullName = firstTestCase.GetPropertyHelper(SailfishTestTypeFullNameDefinition.SailfishTestTypeFullNameDefinitionProperty);
            var assembly = LoadAssemblyFromDll(firstTestCase.Source);
            var testType = assembly.GetType(testTypeFullName, true, true);
            if (testType is null)
            {
                frameworkHandle?.SendMessage(TestMessageLevel.Error, $"Unable to find the following testType: {testTypeFullName}");
                continue;
            }

            var availableVariableSections =
                testCases.Select(x => x.GetPropertyHelper(SailfishFormedVariableSectionDefinition.SailfishFormedVariableSectionDefinitionProperty)).Distinct();

            bool PropertyFilter(PropertySet currentPropertySet)
            {
                var currentVariableSection = currentPropertySet.FormTestCaseVariableSection();
                return availableVariableSections.Contains(currentVariableSection);
            }

            var availableMethods = testCases
                .Select(x => x.GetPropertyHelper(SailfishMethodNameDefinition.SailfishMethodNameDefinitionProperty))
                .Distinct();

            bool MethodFilter(MethodInfo currentMethodInfo)
            {
                var currentMethod = currentMethodInfo.Name;
                return availableMethods.Contains(currentMethod);
            }

            // list of methods with their many variable combos. Each element is a container, which represents a SailfishMethod
            var providerForCurrentTestCases = testInstanceContainerCreator.CreateTestContainerInstanceProviders(testType, PropertyFilter, MethodFilter);

            var totalTestProviderCount = providerForCurrentTestCases.Count - 1;
            var memoryCache = new MemoryCache(MemoryCacheName);
            for (var i = 0; i < providerForCurrentTestCases.Count; i++)
            {
                var testProvider = providerForCurrentTestCases[i];
                var providerPropertiesCacheKey = testProvider.Test.FullName ?? throw new SailfishException($"Failed to read the FullName of {testProvider.Test.Name}");
                var results = engine.ActivateContainer(
                        i,
                        totalTestProviderCount,
                        testProvider,
                        memoryCache,
                        providerPropertiesCacheKey,
                        PreTestResultCallback(testCaseGroup, frameworkHandle),
                        PostTestResultCallback(testCaseGroup, frameworkHandle, cancellationToken),
                        ExceptionCallback(testCaseGroup, frameworkHandle),
                        cancellationToken: cancellationToken)
                    .GetAwaiter().GetResult();
                groupResults.AddRange(results);
            }

            rawResults.Add(new RawExecutionResult(testType, groupResults));
        }

        var compiledResults = executionSummaryCompiler.CompileToSummaries(rawResults, CancellationToken.None);
        consoleWriterFactory.CreateConsoleWriter(frameworkHandle).Present(compiledResults);
    }

    private static Assembly LoadAssemblyFromDll(string dllPath)
    {
        var assembly = Assembly.LoadFile(dllPath);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?
        return assembly;
    }

    private static void LogTestResults(TestExecutionResult result, IMessageLogger? logger)
    {
        foreach (var perf in result.PerformanceTimerResults?.MethodIterationPerformances!)
        {
            logger?.SendMessage(TestMessageLevel.Informational, $"Time: {perf.Duration.ToString()} ms");
        }
    }

    private static Action<TestInstanceContainer?> ExceptionCallback(IGrouping<string, TestCase> testCaseGroup, ITestExecutionRecorder? logger)
    {
        return (container) =>
        {
            if (container is null)
            {
                foreach (var testCase in testCaseGroup)
                {
                    logger?.RecordEnd(testCase, TestOutcome.Failed);
                }
            }
            else
            {
                var currentTestCase = GetTestCaseFromTestCaseGroupMatchingCurrentContainer(container, testCaseGroup);
                logger?.RecordEnd(currentTestCase, TestOutcome.Failed);
            }
        };
    }

    private static Action<TestInstanceContainer> PreTestResultCallback(IGrouping<string, TestCase> testCaseGroup, ITestExecutionRecorder? logger)
    {
        return container =>
        {
            var currentTestCase = GetTestCaseFromTestCaseGroupMatchingCurrentContainer(container, testCaseGroup);
            logger?.RecordStart(currentTestCase);
        };
    }

    private static TestCase GetTestCaseFromTestCaseGroupMatchingCurrentContainer(TestInstanceContainer container, IEnumerable<TestCase> testCaseGroup)
    {
        return testCaseGroup.Single(x => string.Equals(x.DisplayName, container.TestCaseId.DisplayName, StringComparison.InvariantCultureIgnoreCase));
    }

    private Action<TestExecutionResult, TestInstanceContainer> PostTestResultCallback(
        IGrouping<string, TestCase> testCaseGroups,
        ITestExecutionRecorder? logger,
        CancellationToken cancellationToken)
    {
        return (result, container) =>
        {
            if (result.PerformanceTimerResults is null)
            {
                var msg = $"PerformanceTimerResults was null for {container.Type.Name}";
                logger?.SendMessage(TestMessageLevel.Error, msg);
                throw new SailfishException(msg);
            }

            if (result.TestInstanceContainer is null)
            {
                var msg = $"TestInstanceContainer was null for {container.Type.Name}";
                logger?.SendMessage(TestMessageLevel.Error, msg);
                throw new SailfishException(msg);
            }


            var currentTestCase = GetTestCaseFromTestCaseGroupMatchingCurrentContainer(container, testCaseGroups);
            if (result.IsSuccess)
            {
                HandleSuccessfulTestCase(
                    result,
                    currentTestCase,
                    new RawExecutionResult(result.TestInstanceContainer.Type, new List<TestExecutionResult> { result }),
                    logger,
                    cancellationToken);
            }
            else
            {
                HandleFailureTestCase(
                    result,
                    currentTestCase,
                    new RawExecutionResult(result.TestInstanceContainer.Type,
                        result.Exception ?? new Exception($"The exception details were null for {result.TestInstanceContainer.Type.Name}")),
                    logger,
                    cancellationToken);
            }
        };
    }

    void HandleSuccessfulTestCase(TestExecutionResult result, TestCase currentTestCase, RawExecutionResult rawResult, ITestExecutionRecorder? logger, CancellationToken cancellationToken)
    {
        var compiledResult = executionSummaryCompiler.CompileToSummaries(new List<RawExecutionResult>() { rawResult }, cancellationToken).ToList();
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

        testResult.StartTime = result.PerformanceTimerResults?.GlobalStart ?? new DateTimeOffset();
        testResult.EndTime = result.PerformanceTimerResults?.GlobalStop ?? new DateTimeOffset();
        testResult.Duration = TimeSpan.FromMilliseconds(medianTestRuntime);

        testResult.ErrorMessage = result.Exception?.Message;

        var outputs = consoleWriterFactory.CreateConsoleWriter(logger).Present(compiledResult);
        testResult.Messages.Add(new TestResultMessage("Test Result", outputs));

        LogTestResults(result, logger);

        logger?.RecordResult(testResult);
        logger?.RecordEnd(currentTestCase, testResult.Outcome);
    }

    void HandleFailureTestCase(TestExecutionResult result, TestCase currentTestCase, RawExecutionResult rawResult, ITestExecutionRecorder? logger, CancellationToken cancellationToken)
    {
        var testResult = new TestResult(currentTestCase);

        if (result.Exception is not null)
        {
            testResult.ErrorMessage = result.Exception.Message;
            testResult.ErrorStackTrace = result.Exception.StackTrace;
        }

        testResult.Outcome = result.StatusCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;
        testResult.DisplayName = currentTestCase.DisplayName;

        testResult.StartTime = result.PerformanceTimerResults?.GlobalStart ?? new DateTimeOffset();
        testResult.EndTime = result.PerformanceTimerResults?.GlobalStop ?? new DateTimeOffset();
        testResult.Duration = TimeSpan.Zero;

        testResult.ErrorMessage = result.Exception?.Message;

        logger?.SendMessage(TestMessageLevel.Error, rawResult.Exception?.Message ?? $"Exception was null for {rawResult.TestType.Name}");
        logger?.RecordResult(testResult);
        logger?.RecordEnd(currentTestCase, testResult.Outcome);
    }
}