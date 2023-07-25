using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using Autofac.Builder;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Analysis;
using Sailfish.Attributes;
using Sailfish.Contracts.Public;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.TestProperties;
using Sailfish.TestAdapter.TestSettingsParser;
using Sailfish.Utils;


namespace Sailfish.TestAdapter.Execution;

internal class TestAdapterExecutionProgram : ITestAdapterExecutionProgram
{
    private readonly ITestInstanceContainerCreator testInstanceContainerCreator;
    private readonly IConsoleWriterFactory consoleWriterFactory;
    private readonly IExecutionSummaryCompiler executionSummaryCompiler;
    private readonly ISailfishExecutionEngine engine;
    private readonly IMediator mediator;
    private readonly ITestComputer testComputer;
    private readonly ITestResultPresenter testResultPresenter;
    private const string MemoryCacheName = "GlobalStateMemoryCache";

    public TestAdapterExecutionProgram(
        ITestInstanceContainerCreator testInstanceContainerCreator,
        IConsoleWriterFactory consoleWriterFactory,
        IExecutionSummaryCompiler executionSummaryCompiler,
        ISailfishExecutionEngine engine,
        IMediator mediator,
        ITestComputer testComputer,
        ITestResultPresenter testResultPresenter)
    {
        this.testInstanceContainerCreator = testInstanceContainerCreator;
        this.consoleWriterFactory = consoleWriterFactory;
        this.executionSummaryCompiler = executionSummaryCompiler;
        this.engine = engine;
        this.mediator = mediator;
        this.testComputer = testComputer;
        this.testResultPresenter = testResultPresenter;
    }

    public void Run(List<TestCase> testCases, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken)
    {
        if (testCases.Count == 0)
        {
            frameworkHandle?.SendMessage(TestMessageLevel.Informational, "No Sailfish tests were discovered");
            return;
        }

        var rawExecutionResults = new List<RawExecutionResult>();
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

            rawExecutionResults.Add(new RawExecutionResult(testType, groupResults));
        }

        var executionSummaries = executionSummaryCompiler.CompileToSummaries(rawExecutionResults, cancellationToken).ToList();
        consoleWriterFactory.CreateConsoleWriter(frameworkHandle).Present(executionSummaries);

        if (frameworkHandle is not null)
        {
            // Let the test platform know that it should tear down the test host process
            frameworkHandle.EnableShutdownAfterTestRun = true;
        }

        if (!AnalysisEnabled(out var testSettings)) return;
        if (testSettings.TestSettings.Disabled) return;

        var mappedTestSettings = MapToTestSettings(testSettings.TestSettings);
        var runSettingsBuilder = RunSettingsBuilder.CreateBuilder();
        if (testSettings?.TestSettings.ResultsDirectory is not null)
        {
            runSettingsBuilder.WithLocalOutputDirectory(testSettings.TestSettings.ResultsDirectory);
        }

        var settings = runSettingsBuilder
            .CreateTrackingFiles()
            .WithAnalysis()
            .WithAnalysisTestSettings(mappedTestSettings)
            .Build();

        var trackingDir = GetRunSettingsTrackingDirectoryPath(settings);

        var timeStamp = DateTime.Now;
        testResultPresenter.PresentResults(executionSummaries, timeStamp, trackingDir, settings, cancellationToken).Wait(cancellationToken);

        // complexity analysis
        // get results and find tests with the complexity attribute
        // get all variable combos (again?) and then for each complexity property find each variable set where the the off-taget variavels match teh first.

        foreach (var executionSummary in executionSummaries)
        {
            var testClass = executionSummary.Type;

            var complexityProperties = testClass
                .GetProperties()
                .Where(x => x.GetCustomAttributes<SailfishVariableAttribute>().Single().IsComplexityVariable())
                .ToList();

            var propertySetGenerator = new PropertySetGenerator(new ParameterCombinator(), new IterationVariableRetriever());
            var propertySets = propertySetGenerator.GenerateSailfishVariableSets(testClass, out var variableProperties).ToArray();
            // property sets should be an array the same length as the test cases from thi executionSummary.CompiledResults


            // like (N: 1, X: 2, Z: 8)
            var referencePropertySet = propertySets.First().FormTestCaseVariableSection();
            // so find all testCase result where N: 1, 2, 3 but X always 2, and Z always 8
            
            
            
            
            
            

            propertySets.First().DisplayNameHelper.CreateTestCaseId(testClass,)


            foreach (var complexityProperty in complexityProperties)
            {
                foreach (var currentComplexityPropertyVal in complexityProperty.GetCustomAttributes<SailfishVariableAttribute>().Single().GetVariables().Cast<int>().ToList())
                {
                    var referenceOffTargetVars = executionSummary
                        .CompiledResults
                        .First(x => (int)(x.TestCaseId?.TestCaseVariables.Variables.First().Value!) == currentComplexityPropertyVal);

                    referenceOffTargetVars.TestCaseId.TestCaseVariables
                }
            }
        }


        // before and after analysis
        var testResultAnalyzer = new AdapterTestResultAnalyzer(
            mediator,
            consoleWriterFactory.CreateConsoleWriter(frameworkHandle),
            testComputer,
            new TestResultTableContentFormatter());
        testResultAnalyzer.Analyze(timeStamp, settings, trackingDir, cancellationToken).Wait(cancellationToken);
    }

    private static string GetRunSettingsTrackingDirectoryPath(IRunSettings runSettings)
    {
        string trackingDirectoryPath;
        if (string.IsNullOrEmpty(runSettings.LocalOutputDirectory) || string.IsNullOrWhiteSpace(runSettings.LocalOutputDirectory))
        {
            trackingDirectoryPath = DefaultFileSettings.DefaultTrackingDirectory;
        }
        else
        {
            trackingDirectoryPath = Path.Join(runSettings.LocalOutputDirectory, DefaultFileSettings.DefaultTrackingDirectory);
        }

        if (!Directory.Exists(trackingDirectoryPath))
        {
            Directory.CreateDirectory(trackingDirectoryPath);
        }

        return trackingDirectoryPath;
    }

    private TestSettings MapToTestSettings(SailfishTestSettings settings)
    {
        if (settings?.Resolution is not null)
        {
            // TODO: Modify this when we impl resolution settings throughout (or ditch the idea)
            // settingsBuilder.WithResolution(settings.Resolution);
        }

        var testSettings = new TestSettings();
        if (settings?.TestType is not null)
        {
            testSettings.SetTestType(settings.TestType);
        }

        if (settings?.UseInnerQuartile is not null)
        {
            testSettings.SetUseInnerQuartile(settings.UseInnerQuartile);
        }

        if (settings?.Alpha is not null)
        {
            testSettings.SetAlpha(settings.Alpha);
        }

        if (settings?.Round is not null)
        {
            testSettings.SetRound(settings.Round);
        }

        return testSettings;
    }

    private bool AnalysisEnabled(out SailfishSettings testSettings)
    {
        try
        {
            var settingsFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(
                ".sailfish.json",
                Directory.GetCurrentDirectory(),
                6);
            testSettings = SailfishSettingsParser.Parse(settingsFile.FullName);
            return true;
        }
        catch (Exception ex)
        {
            testSettings = new SailfishSettings();
            return false;
        }
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
            var timeResult = perf.GetDurationFromTicks().MilliSeconds;
            logger?.SendMessage(TestMessageLevel.Informational, $"Time: {timeResult.Duration.ToString()} {timeResult.TimeScale.ToString().ToLowerInvariant()}");
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

    private void HandleSuccessfulTestCase(TestExecutionResult result, TestCase currentTestCase, RawExecutionResult rawResult, ITestExecutionRecorder? logger,
        CancellationToken cancellationToken)
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
        testResult.Duration = TimeSpan.FromMilliseconds(double.IsNaN(medianTestRuntime) ? 0 : medianTestRuntime);

        testResult.ErrorMessage = result.Exception?.Message;

        var outputs = consoleWriterFactory.CreateConsoleWriter(logger).Present(compiledResult);

        testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, outputs));

        if (result.Exception is not null)
        {
            testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, result.Exception?.Message));
        }


        LogTestResults(result, logger);

        logger?.RecordEnd(currentTestCase, testResult.Outcome);
        logger?.RecordResult(testResult);
    }

    private static void HandleFailureTestCase(
        TestExecutionResult result,
        TestCase currentTestCase,
        RawExecutionResult rawResult,
        ITestExecutionRecorder? logger,
        CancellationToken cancellationToken)
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

        testResult.Messages.Clear();
        testResult.ErrorMessage = result.Exception?.Message;

        foreach (var exception in rawResult.Exceptions)
        {
            logger?.SendMessage(TestMessageLevel.Error, "----- Exception -----");
            logger?.SendMessage(TestMessageLevel.Error, exception.Message);
        }

        logger?.RecordResult(testResult);
        logger?.RecordEnd(currentTestCase, testResult.Outcome);
    }
}