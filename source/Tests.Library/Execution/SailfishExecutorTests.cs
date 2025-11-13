using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.Presentation;
using Shouldly;
using Xunit;
using System.Reflection;


namespace Tests.Library.Execution;

public class SailfishExecutorTests
{
    private readonly IMediator mediator = Substitute.For<IMediator>();
    private readonly ISailFishTestExecutor sailFishTestExecutor = Substitute.For<ISailFishTestExecutor>();
    private readonly ITestCollector testCollector = Substitute.For<ITestCollector>();
    private readonly ITestFilter testFilter = Substitute.For<ITestFilter>();
    private readonly IClassExecutionSummaryCompiler classExecutionSummaryCompiler = Substitute.For<IClassExecutionSummaryCompiler>();
    private readonly IExecutionSummaryWriter executionSummaryWriter = Substitute.For<IExecutionSummaryWriter>();
    private readonly ISailDiffInternal sailDiff = Substitute.For<ISailDiffInternal>();
    private readonly IScaleFishInternal scaleFish = Substitute.For<IScaleFishInternal>();
    private readonly IRunSettings runSettings = Substitute.For<IRunSettings>();
    private readonly ILogger logger = Substitute.For<ILogger>();

    [Fact]
    public async Task Run_WithValidTests_ReturnsSuccessfulResult()
    {
        // Arrange
        var testType = typeof(object);
        var testInitResult = TestInitializationResult.CreateSuccess(new[] { testType });
        var testClassResultGroup = new TestClassResultGroup(testType, new List<TestCaseExecutionResult>());
        var classExecutionSummary = new ClassExecutionSummary(testType, new ExecutionSettings(), new List<ICompiledTestCaseResult>());

        testFilter.FilterAndValidate(Arg.Any<IEnumerable<Type>>(), Arg.Any<IEnumerable<string>>()).Returns(testInitResult);
        sailFishTestExecutor.Execute(Arg.Any<IEnumerable<Type>>(), Arg.Any<CancellationToken>()).Returns(new List<TestClassResultGroup> { testClassResultGroup });
        classExecutionSummaryCompiler.CompileToSummaries(Arg.Any<IEnumerable<TestClassResultGroup>>()).Returns(new List<IClassExecutionSummary> { classExecutionSummary }.AsEnumerable());
        runSettings.TestNames.Returns(new List<string>());
        runSettings.TestLocationAnchors.Returns(new List<Type>());
        runSettings.Seed.Returns((int?)null);
        runSettings.Args.Returns(new Sailfish.Extensions.Types.OrderedDictionary());
        runSettings.RunSailDiff.Returns(false);
        runSettings.RunScaleFish.Returns(false);

        var executor = new SailfishExecutor(mediator, sailFishTestExecutor, testCollector, testFilter, classExecutionSummaryCompiler, executionSummaryWriter, sailDiff, scaleFish, runSettings, logger);

        // Act
        var result = await executor.Run(CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ExecutionSummaries.ShouldNotBeEmpty();
        await executionSummaryWriter.Received(1).Write(Arg.Any<List<IClassExecutionSummary>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_WithTestDiscoveryErrors_ReturnsInvalidResult()
    {
        // Arrange
        var errors = new Dictionary<string, List<string>> { { "Error reason", new List<string> { "TestName1", "TestName2" } } };
        var testInitResult = TestInitializationResult.CreateFailure(new Type[0], errors);

        testFilter.FilterAndValidate(Arg.Any<IEnumerable<Type>>(), Arg.Any<IEnumerable<string>>()).Returns(testInitResult);
        runSettings.TestNames.Returns(new List<string>());
        runSettings.TestLocationAnchors.Returns(new List<Type>());

        var executor = new SailfishExecutor(mediator, sailFishTestExecutor, testCollector, testFilter, classExecutionSummaryCompiler, executionSummaryWriter, sailDiff, scaleFish, runSettings, logger);

        // Act
        var result = await executor.Run(CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeEmpty();
        result.Exceptions.Count().ShouldBe(2);
    }

    [Fact]
    public async Task Run_WithSeedInRunSettings_RandomizesTestOrder()
    {
        // Arrange
        var testType1 = typeof(object);
        var testType2 = typeof(string);
        var testInitResult = TestInitializationResult.CreateSuccess(new[] { testType1, testType2 });
        var classExecutionSummary = new ClassExecutionSummary(testType1, new ExecutionSettings(), new List<ICompiledTestCaseResult>());

        testFilter.FilterAndValidate(Arg.Any<IEnumerable<Type>>(), Arg.Any<IEnumerable<string>>()).Returns(testInitResult);
        sailFishTestExecutor.Execute(Arg.Any<IEnumerable<Type>>(), Arg.Any<CancellationToken>()).Returns(new List<TestClassResultGroup>());
        classExecutionSummaryCompiler.CompileToSummaries(Arg.Any<IEnumerable<TestClassResultGroup>>()).Returns(new List<IClassExecutionSummary> { classExecutionSummary }.AsEnumerable());
        runSettings.TestNames.Returns(new List<string>());
        runSettings.TestLocationAnchors.Returns(new List<Type>());
        runSettings.Seed.Returns(42);
        runSettings.Args.Returns(new Sailfish.Extensions.Types.OrderedDictionary());
        runSettings.RunSailDiff.Returns(false);
        runSettings.RunScaleFish.Returns(false);

        var executor = new SailfishExecutor(mediator, sailFishTestExecutor, testCollector, testFilter, classExecutionSummaryCompiler, executionSummaryWriter, sailDiff, scaleFish, runSettings, logger);

        // Act
        var result = await executor.Run(CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        logger.Received(1).Log(Arg.Any<LogLevel>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task Run_WithSailDiffEnabled_CallsSailDiffAnalyze()
    {
        // Arrange
        var testType = typeof(object);
        var testInitResult = TestInitializationResult.CreateSuccess(new[] { testType });
        var classExecutionSummary = new ClassExecutionSummary(testType, new ExecutionSettings(), new List<ICompiledTestCaseResult>());

        testFilter.FilterAndValidate(Arg.Any<IEnumerable<Type>>(), Arg.Any<IEnumerable<string>>()).Returns(testInitResult);
        sailFishTestExecutor.Execute(Arg.Any<IEnumerable<Type>>(), Arg.Any<CancellationToken>()).Returns(new List<TestClassResultGroup>());
        classExecutionSummaryCompiler.CompileToSummaries(Arg.Any<IEnumerable<TestClassResultGroup>>()).Returns(new List<IClassExecutionSummary> { classExecutionSummary }.AsEnumerable());
        runSettings.TestNames.Returns(new List<string>());
        runSettings.TestLocationAnchors.Returns(new List<Type>());
        runSettings.Seed.Returns((int?)null);
        runSettings.Args.Returns(new Sailfish.Extensions.Types.OrderedDictionary());
        runSettings.RunSailDiff.Returns(true);
        runSettings.RunScaleFish.Returns(false);

        var executor = new SailfishExecutor(mediator, sailFishTestExecutor, testCollector, testFilter, classExecutionSummaryCompiler, executionSummaryWriter, sailDiff, scaleFish, runSettings, logger);

        // Act
        await executor.Run(CancellationToken.None);

        // Assert
        await sailDiff.Received(1).Analyze(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_WithScaleFishEnabled_CallsScaleFishAnalyze()
    {
        // Arrange
        var testType = typeof(object);
        var testInitResult = TestInitializationResult.CreateSuccess(new[] { testType });
        var classExecutionSummary = new ClassExecutionSummary(testType, new ExecutionSettings(), new List<ICompiledTestCaseResult>());

        testFilter.FilterAndValidate(Arg.Any<IEnumerable<Type>>(), Arg.Any<IEnumerable<string>>()).Returns(testInitResult);
        sailFishTestExecutor.Execute(Arg.Any<IEnumerable<Type>>(), Arg.Any<CancellationToken>()).Returns(new List<TestClassResultGroup>());
        classExecutionSummaryCompiler.CompileToSummaries(Arg.Any<IEnumerable<TestClassResultGroup>>()).Returns(new List<IClassExecutionSummary> { classExecutionSummary }.AsEnumerable());
        runSettings.TestNames.Returns(new List<string>());
        runSettings.TestLocationAnchors.Returns(new List<Type>());
        runSettings.Seed.Returns((int?)null);
        runSettings.Args.Returns(new Sailfish.Extensions.Types.OrderedDictionary());
        runSettings.RunSailDiff.Returns(false);
        runSettings.RunScaleFish.Returns(true);

        var executor = new SailfishExecutor(mediator, sailFishTestExecutor, testCollector, testFilter, classExecutionSummaryCompiler, executionSummaryWriter, sailDiff, scaleFish, runSettings, logger);

        // Act
        await executor.Run(CancellationToken.None);

        // Assert
        await scaleFish.Received(1).Analyze(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_PublishesTestRunCompletedNotification()
    {
        // Arrange
        var testType = typeof(object);
        var testInitResult = TestInitializationResult.CreateSuccess(new[] { testType });
        var classExecutionSummary = new ClassExecutionSummary(testType, new ExecutionSettings(), new List<ICompiledTestCaseResult>());

        testFilter.FilterAndValidate(Arg.Any<IEnumerable<Type>>(), Arg.Any<IEnumerable<string>>()).Returns(testInitResult);
        sailFishTestExecutor.Execute(Arg.Any<IEnumerable<Type>>(), Arg.Any<CancellationToken>()).Returns(new List<TestClassResultGroup>());
        classExecutionSummaryCompiler.CompileToSummaries(Arg.Any<IEnumerable<TestClassResultGroup>>()).Returns(new List<IClassExecutionSummary> { classExecutionSummary }.AsEnumerable());
        runSettings.TestNames.Returns(new List<string>());
        runSettings.TestLocationAnchors.Returns(new List<Type>());
        runSettings.Seed.Returns((int?)null);
        runSettings.Args.Returns(new Sailfish.Extensions.Types.OrderedDictionary());
        runSettings.RunSailDiff.Returns(false);
        runSettings.RunScaleFish.Returns(false);

        var executor = new SailfishExecutor(mediator, sailFishTestExecutor, testCollector, testFilter, classExecutionSummaryCompiler, executionSummaryWriter, sailDiff, scaleFish, runSettings, logger);

        // Act
        await executor.Run(CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(Arg.Any<TestRunCompletedNotification>(), Arg.Any<CancellationToken>());

        [Fact]
        public async Task Run_WithSeedInArgs_RandomizesOrderAndLogs()
        {
            // Arrange
            var t1 = typeof(object);
            var t2 = typeof(string);
            var t3 = typeof(int);
            var testInitResult = TestInitializationResult.CreateSuccess(new[] { t1, t2, t3 });
            var classExecutionSummary = new ClassExecutionSummary(t1, new ExecutionSettings(), new List<ICompiledTestCaseResult>());

            testFilter.FilterAndValidate(Arg.Any<IEnumerable<Type>>(), Arg.Any<IEnumerable<string>>()).Returns(testInitResult);
            sailFishTestExecutor.Execute(Arg.Any<IEnumerable<Type>>(), Arg.Any<CancellationToken>()).Returns(new List<TestClassResultGroup>());
            classExecutionSummaryCompiler.CompileToSummaries(Arg.Any<IEnumerable<TestClassResultGroup>>())
                .Returns(new List<IClassExecutionSummary> { classExecutionSummary }.AsEnumerable());

            runSettings.TestNames.Returns(new List<string>());
            runSettings.TestLocationAnchors.Returns(new List<Type>());
            runSettings.Seed.Returns((int?)null);
            var args = new Sailfish.Extensions.Types.OrderedDictionary();
            args.Add("seed", "123");
            runSettings.Args.Returns(args);
            runSettings.RunSailDiff.Returns(false);
            runSettings.RunScaleFish.Returns(false);

            var executor = new SailfishExecutor(mediator, sailFishTestExecutor, testCollector, testFilter, classExecutionSummaryCompiler, executionSummaryWriter, sailDiff, scaleFish, runSettings, logger);

            // Act
            var result = await executor.Run(CancellationToken.None);

            // Assert
            result.IsValid.ShouldBeTrue();
            logger.Received(1).Log(
                LogLevel.Information,
                Arg.Is<string>(s => s.Contains("Randomized test class execution order with seed")),
                Arg.Is<object[]>(os => os.Length == 1 && os[0] is int i && i == 123));
        }

        [Theory]
        [InlineData("seed")]
        [InlineData("SEED")]
        [InlineData("randomseed")]
        [InlineData("RandomSeed")]
        [InlineData("rng")]
        [InlineData("RNG")]
        public void TryParseSeed_ParsesKnownKeys(string key)
        {
            // Arrange
            var args = new Sailfish.Extensions.Types.OrderedDictionary();
            args.Add(key, "987");

            var method = typeof(Sailfish.Execution.SailfishExecutor)
                .GetMethod("TryParseSeed", BindingFlags.Static | BindingFlags.NonPublic);
            method.ShouldNotBeNull();

            // Act
            var value = (int?)method!.Invoke(null, new object[] { args });

            // Assert
            value.ShouldBe(987);
        }

        [Fact]
        public void TryParseSeed_ReturnsNull_WhenMissingOrInvalid()
        {
            var method = typeof(Sailfish.Execution.SailfishExecutor)
                .GetMethod("TryParseSeed", BindingFlags.Static | BindingFlags.NonPublic);
            method.ShouldNotBeNull();

            // Missing key
            var argsEmpty = new Sailfish.Extensions.Types.OrderedDictionary();
            var resEmpty = (int?)method!.Invoke(null, new object[] { argsEmpty });
            resEmpty.ShouldBeNull();

            // Invalid value
            var argsInvalid = new Sailfish.Extensions.Types.OrderedDictionary();
            argsInvalid.Add("seed", "not-an-int");
            var resInvalid = (int?)method!.Invoke(null, new object[] { argsInvalid });
            resInvalid.ShouldBeNull();
        }

        [Fact]
        public async Task Run_WithTestDiscoveryErrors_LogsErrorSummaryAndDetails()
        {
            // Arrange
            var errors = new Dictionary<string, List<string>>
            {
                { "Error reason", new List<string> { "TestName1", "TestName2" } }
            };
            var testInitResult = TestInitializationResult.CreateFailure(Array.Empty<Type>(), errors);

            testFilter.FilterAndValidate(Arg.Any<IEnumerable<Type>>(), Arg.Any<IEnumerable<string>>()).Returns(testInitResult);
            runSettings.TestNames.Returns(new List<string>());
            runSettings.TestLocationAnchors.Returns(new List<Type>());

            var executor = new SailfishExecutor(mediator, sailFishTestExecutor, testCollector, testFilter, classExecutionSummaryCompiler, executionSummaryWriter, sailDiff, scaleFish, runSettings, logger);

            // Act
            await executor.Run(CancellationToken.None);

            // Assert summary line
            logger.Received().Log(
                LogLevel.Error,
                Arg.Is<string>(s => s.Contains("errors encountered while discovering tests")),
                Arg.Is<object[]>(os => os.Length == 1 && os[0] is int n && n == 2));

            // Assert reason and individual names were logged
            logger.Received().Log(LogLevel.Error, Arg.Is<string>(s => s.Contains("{Reason}")), Arg.Any<object[]>());
            logger.Received().Log(LogLevel.Error, Arg.Is<string>(s => s.Contains("--- {TestName}")), Arg.Any<object[]>());
        }

    }
}

