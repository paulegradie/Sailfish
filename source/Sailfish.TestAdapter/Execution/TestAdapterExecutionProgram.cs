using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Analysis;
using Sailfish.Analysis.ComplexityEstimation;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.CsvMaps;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.TestProperties;
using Sailfish.TestAdapter.TestSettingsParser;


namespace Sailfish.TestAdapter.Execution;

internal class TestAdapterExecutionProgram : ITestAdapterExecutionProgram
{
    private readonly IMarkdownTableConverter markdownTableConverter;
    private readonly IComplexityComputer complexityComputer;
    private readonly ITestInstanceContainerCreator testInstanceContainerCreator;
    private readonly IConsoleWriterFactory consoleWriterFactory;
    private readonly IExecutionSummaryCompiler executionSummaryCompiler;
    private readonly ISailfishExecutionEngine engine;
    private readonly IMediator mediator;
    private readonly ITestComputer testComputer;
    private readonly ITestResultPresenter testResultPresenter;
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;
    private readonly IFileIo fileIo;
    private readonly ITestResultTableContentFormatter testResultTableContentFormatter;
    private const string MemoryCacheName = "GlobalStateMemoryCache";

    private TestSettings? testSettings;
    private List<DescriptiveStatisticsResult>? preloadedLastRunIfAvailable;
    private IRunSettings? runSettings;
    private string? trackingDir;

    public TestAdapterExecutionProgram(
        IMarkdownTableConverter markdownTableConverter,
        IComplexityComputer complexityComputer,
        ITestInstanceContainerCreator testInstanceContainerCreator,
        IConsoleWriterFactory consoleWriterFactory,
        IExecutionSummaryCompiler executionSummaryCompiler,
        ISailfishExecutionEngine engine,
        IMediator mediator,
        ITestComputer testComputer,
        ITestResultPresenter testResultPresenter,
        ITrackingFileDirectoryReader trackingFileDirectoryReader,
        IFileIo fileIo,
        ITestResultTableContentFormatter testResultTableContentFormatter)
    {
        this.markdownTableConverter = markdownTableConverter;
        this.complexityComputer = complexityComputer;
        this.testInstanceContainerCreator = testInstanceContainerCreator;
        this.consoleWriterFactory = consoleWriterFactory;
        this.executionSummaryCompiler = executionSummaryCompiler;
        this.engine = engine;
        this.mediator = mediator;
        this.testComputer = testComputer;
        this.testResultPresenter = testResultPresenter;
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
        this.fileIo = fileIo;
        this.testResultTableContentFormatter = testResultTableContentFormatter;
    }

    public void Run(List<TestCase> testCases, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken)
    {
        if (testCases.Count == 0)
        {
            frameworkHandle?.SendMessage(TestMessageLevel.Informational, "No Sailfish tests were discovered");
            return;
        }

        var statisticalTestingEnabled = AnalysisEnabled(out var parsedSettings);
        if (statisticalTestingEnabled)
        {
            var runSettingsBuilder = RunSettingsBuilder.CreateBuilder();
            if (parsedSettings.TestSettings.ResultsDirectory is not null)
            {
                runSettingsBuilder.WithLocalOutputDirectory(parsedSettings.TestSettings.ResultsDirectory);
            }

            runSettings = runSettingsBuilder
                .CreateTrackingFiles()
                .WithAnalysis()
                .WithAnalysisTestSettings(testSettings!)
                .Build();
            trackingDir = GetRunSettingsTrackingDirectoryPath(runSettings);
            testSettings = MapToTestSettings(parsedSettings.TestSettings);

            var trackingFiles =
                trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(trackingDir,
                    ascending: false);
            var latestRun = trackingFiles.Count switch
            {
                0 => null,
                1 => trackingFiles.Single(),
                _ => trackingFiles.First()
            };

            if (latestRun is not null)
            {
                preloadedLastRunIfAvailable = fileIo
                    .ReadCsvFile<DescriptiveStatisticsResultCsvMap, DescriptiveStatisticsResult>(latestRun,
                        cancellationToken)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        var rawExecutionResults = new List<(string, RawExecutionResult)>();
        var testCaseGroups = testCases
            .GroupBy(testCase =>
                testCase.GetPropertyHelper(
                    SailfishTestTypeFullNameDefinition.SailfishTestTypeFullNameDefinitionProperty));

        foreach (var testCaseGroup in testCaseGroups)
        {
            var groupResults = new List<TestExecutionResult>();

            var firstTestCase = testCaseGroup.First();
            var testTypeFullName =
                firstTestCase.GetPropertyHelper(SailfishTestTypeFullNameDefinition
                    .SailfishTestTypeFullNameDefinitionProperty);
            var assembly = LoadAssemblyFromDll(firstTestCase.Source);
            var testType = assembly.GetType(testTypeFullName, true, true);
            if (testType is null)
            {
                frameworkHandle?.SendMessage(TestMessageLevel.Error,
                    $"Unable to find the following testType: {testTypeFullName}");
                continue;
            }

            var availableVariableSections =
                testCases.Select(x =>
                    x.GetPropertyHelper(SailfishFormedVariableSectionDefinition
                        .SailfishFormedVariableSectionDefinitionProperty)).Distinct();

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
            var providerForCurrentTestCases =
                testInstanceContainerCreator
                    .CreateTestContainerInstanceProviders(
                        testType,
                        PropertyFilter,
                        MethodFilter);

            var totalTestProviderCount = providerForCurrentTestCases.Count - 1;
            var memoryCache = new MemoryCache(MemoryCacheName);
            for (var i = 0; i < providerForCurrentTestCases.Count; i++)
            {
                var testProvider = providerForCurrentTestCases[i];
                var providerPropertiesCacheKey = testProvider.Test.FullName ??
                                                 throw new SailfishException(
                                                     $"Failed to read the FullName of {testProvider.Test.Name}");
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

            rawExecutionResults.Add((testCaseGroup.Key, new RawExecutionResult(testType, groupResults)));
        }

        var executionSummaries = executionSummaryCompiler
            .CompileToSummaries(rawExecutionResults.Select(x => x.Item2), cancellationToken)
            .ToList();
        var consoleWriter = consoleWriterFactory.CreateConsoleWriter(frameworkHandle);
        consoleWriter.Present(executionSummaries);

        if (frameworkHandle is not null)
        {
            // Let the test platform know that it should tear down the test host process
            frameworkHandle.EnableShutdownAfterTestRun = true;
        }

        if (!statisticalTestingEnabled) return;
        if (parsedSettings.TestSettings.Disabled) return;

        var timeStamp = DateTime.Now;
        testResultPresenter
            .PresentResults(executionSummaries, timeStamp, trackingDir!, runSettings!, cancellationToken)
            .Wait(cancellationToken);

        ComputeComplexityAnalysis(executionSummaries, consoleWriter);
        ComputeBeforeAndAfterAnalysis(frameworkHandle, cancellationToken, timeStamp);
    }

    private void ComputeBeforeAndAfterAnalysis(IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken,
        DateTime timeStamp)
    {
        // before and after analysis
        var testResultAnalyzer = new AdapterTestResultAnalyzer(
            mediator,
            consoleWriterFactory.CreateConsoleWriter(frameworkHandle),
            testComputer,
            new TestResultTableContentFormatter());
        testResultAnalyzer.Analyze(timeStamp, runSettings!, trackingDir!, cancellationToken).Wait(cancellationToken);
    }

    private void ComputeComplexityAnalysis(List<IExecutionSummary> executionSummaries, ConsoleWriter consoleWriter)
    {
        try
        {
            var complexityResults = complexityComputer.AnalyzeComplexity(executionSummaries);
            var complexityMarkdown = markdownTableConverter.ConvertComplexityResultToMarkdown(complexityResults);
            consoleWriter.WriteString(complexityMarkdown);
        }
        catch (Exception ex)
        {
            consoleWriter.WriteString(ex.Message);
        }
    }

    private static string GetRunSettingsTrackingDirectoryPath(IRunSettings runSettings)
    {
        string trackingDirectoryPath;
        if (string.IsNullOrEmpty(runSettings.LocalOutputDirectory) ||
            string.IsNullOrWhiteSpace(runSettings.LocalOutputDirectory))
        {
            trackingDirectoryPath = DefaultFileSettings.DefaultTrackingDirectory;
        }
        else
        {
            trackingDirectoryPath =
                Path.Join(runSettings.LocalOutputDirectory, DefaultFileSettings.DefaultTrackingDirectory);
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

        var mappedSettings = new TestSettings();
        if (settings?.TestType is not null)
        {
            mappedSettings.SetTestType(settings.TestType);
        }

        if (settings?.UseInnerQuartile is not null)
        {
            mappedSettings.SetUseInnerQuartile(settings.UseInnerQuartile);
        }

        if (settings?.Alpha is not null)
        {
            mappedSettings.SetAlpha(settings.Alpha);
        }

        if (settings?.Round is not null)
        {
            mappedSettings.SetRound(settings.Round);
        }

        return mappedSettings;
    }

    private static bool AnalysisEnabled(out SailfishSettings parsedSettings)
    {
        try
        {
            var settingsFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(
                ".sailfish.json",
                Directory.GetCurrentDirectory(),
                6);
            parsedSettings = SailfishSettingsParser.Parse(settingsFile.FullName);
            return true;
        }
        catch
        {
            parsedSettings = new SailfishSettings();
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
            logger?.SendMessage(TestMessageLevel.Informational,
                $"Time: {timeResult.Duration.ToString(CultureInfo.InvariantCulture)} {timeResult.TimeScale.ToString().ToLowerInvariant()}");
        }
    }

    private static Action<TestInstanceContainer?> ExceptionCallback(IGrouping<string, TestCase> testCaseGroup,
        ITestExecutionRecorder? logger)
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

    private static Action<TestInstanceContainer> PreTestResultCallback(IGrouping<string, TestCase> testCaseGroup,
        ITestExecutionRecorder? logger)
    {
        return container =>
        {
            var currentTestCase = GetTestCaseFromTestCaseGroupMatchingCurrentContainer(container, testCaseGroup);
            logger?.RecordStart(currentTestCase);
        };
    }

    private static TestCase GetTestCaseFromTestCaseGroupMatchingCurrentContainer(TestInstanceContainer container,
        IEnumerable<TestCase> testCaseGroup)
    {
        return testCaseGroup.Single(x => string.Equals(x.DisplayName, container.TestCaseId.DisplayName,
            StringComparison.InvariantCultureIgnoreCase));
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
                        result.Exception ??
                        new Exception($"The exception details were null for {result.TestInstanceContainer.Type.Name}")),
                    logger,
                    cancellationToken);
            }
        };
    }

    private void HandleSuccessfulTestCase(
        TestExecutionResult result,
        TestCase currentTestCase,
        RawExecutionResult rawResult,
        ITestExecutionRecorder? logger,
        CancellationToken cancellationToken)
    {
        var executionSummary = executionSummaryCompiler
            .CompileToSummaries(new List<RawExecutionResult>() { rawResult }, cancellationToken)
            .Single();
        var medianTestRuntime = executionSummary.CompiledTestCaseResults.Single().DescriptiveStatisticsResult?.Median ??
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

        var formattedExecutionSummary =
            consoleWriterFactory.CreateConsoleWriter(logger).Present(new[] { executionSummary });

        if (preloadedLastRunIfAvailable is not null && testSettings is not null)
        {
            var beforeIds = new[] { result.TestInstanceContainer?.TestCaseId.DisplayName ?? string.Empty };
            var afterIds = new[] { result.TestInstanceContainer?.TestCaseId.DisplayName ?? string.Empty };

            var beforeTestData = new TestData(
                beforeIds,
                preloadedLastRunIfAvailable.Where(x =>
                    x.DisplayName == result.TestInstanceContainer?.TestCaseId.DisplayName));

            var afterTestData = new TestData(afterIds,
                executionSummary.CompiledTestCaseResults
                    .Select(x => x.DescriptiveStatisticsResult!)
                    .Where(x => x.DisplayName == result.TestInstanceContainer?.TestCaseId.DisplayName));

            var testResults = testComputer.ComputeTest(beforeTestData, afterTestData, testSettings);
            var testResultFormats = testResultTableContentFormatter.CreateTableFormats(testResults,
                new TestIds(beforeIds, afterIds), cancellationToken);
            formattedExecutionSummary +=
                "\n\n----------\n\nStatistical Test Results\n\n" + testResultFormats.MarkdownFormat;
        }

        testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory,
            formattedExecutionSummary));

        if (result.Exception is not null)
        {
            testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory,
                result.Exception?.Message));
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