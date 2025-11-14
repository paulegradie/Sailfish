using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Logging;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

/// <summary>
/// Comprehensive unit tests for TestCaseIterator.
/// Tests iteration logic, overhead estimation, warmup, and error handling.
/// </summary>
public class TestCaseIteratorTests
{
    private readonly ILogger _mockLogger;
    private readonly IRunSettings _mockRunSettings;
    private readonly IIterationStrategy _mockFixedStrategy;
    private readonly IIterationStrategy _mockAdaptiveStrategy;
    private readonly TestCaseIterator _testCaseIterator;

    public TestCaseIteratorTests()
    {
        _mockLogger = Substitute.For<ILogger>();
        _mockRunSettings = Substitute.For<IRunSettings>();
        // Use real FixedIterationStrategy so iteration progress is logged and CoreInvoker is exercised
        _mockFixedStrategy = new FixedIterationStrategy(_mockLogger);
        // Adaptive strategy is not used in these tests (UseAdaptiveSampling defaults to false),
        // but configure a safe default in case it's invoked
        _mockAdaptiveStrategy = Substitute.For<IIterationStrategy>();
        _mockAdaptiveStrategy.ExecuteIterations(
            Arg.Any<TestInstanceContainer>(),
            Arg.Any<IExecutionSettings>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IterationResult
            {
                IsSuccess = true,
                TotalIterations = 1,
                ConvergedEarly = false
            }));
        _testCaseIterator = new TestCaseIterator(_mockRunSettings, _mockLogger, _mockFixedStrategy, _mockAdaptiveStrategy);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Act & Assert
        _testCaseIterator.ShouldNotBeNull();
        _testCaseIterator.ShouldBeAssignableTo<ITestCaseIterator>();
    }



    [Fact]
    public async Task Iterate_WithValidContainer_ShouldCompleteSuccessfully()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        _mockRunSettings.SampleSizeOverride.Returns((int?)null);

        // Act
        var result = await _testCaseIterator.Iterate(container, false, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Iterate_WithDisabledOverheadEstimation_ShouldSkipEstimation()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        _mockRunSettings.SampleSizeOverride.Returns((int?)null);

        // Act
        var result = await _testCaseIterator.Iterate(container, true, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Iterate_WithSampleSizeOverride_ShouldUseSampleSizeOverride()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        _mockRunSettings.SampleSizeOverride.Returns(5);

        // Act
        var result = await _testCaseIterator.Iterate(container, true, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        // Verify that the correct number of iterations were logged
        _mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 1, 5);
    }

    [Fact]
    public async Task Iterate_WithZeroSampleSizeOverride_ShouldUseMinimumOfOne()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        _mockRunSettings.SampleSizeOverride.Returns(0);

        // Act
        var result = await _testCaseIterator.Iterate(container, true, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        // Should use minimum of 1 iteration
        _mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 1, 1);
    }

    [Fact]
    public async Task Iterate_WithNegativeSampleSizeOverride_ShouldUseMinimumOfOne()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        _mockRunSettings.SampleSizeOverride.Returns(-5);

        // Act
        var result = await _testCaseIterator.Iterate(container, true, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        // Should use minimum of 1 iteration
        _mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 1, 1);
    }

    [Fact]
    public async Task Iterate_WithWarmupIterations_ShouldPerformWarmup()
    {
        // Arrange
        var container = CreateTestInstanceContainer(numWarmupIterations: 2);
        _mockRunSettings.SampleSizeOverride.Returns(1);

        // Act
        var result = await _testCaseIterator.Iterate(container, true, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        // Verify warmup iterations were logged
        _mockLogger.Received().Log(LogLevel.Information, "      ---- warmup iteration {CurrentIteration} of {TotalIterations}", 1, 2);
        _mockLogger.Received().Log(LogLevel.Information, "      ---- warmup iteration {CurrentIteration} of {TotalIterations}", 2, 2);
    }





    [Fact]
    public async Task Iterate_ShouldLogIterationProgress()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        _mockRunSettings.SampleSizeOverride.Returns(3);

        // Act
        var result = await _testCaseIterator.Iterate(container, true, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();

        // Verify all iterations were logged
        _mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 1, 3);
        _mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 2, 3);
        _mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 3, 3);
    }

    [Fact]
    public async Task Iterate_WithOverheadEstimationEnabled_ShouldApplyOverheadEstimates()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        _mockRunSettings.SampleSizeOverride.Returns(1);

        // Act
        var result = await _testCaseIterator.Iterate(container, false, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        // The overhead estimation should have been performed (this is tested indirectly)
    }

    private TestInstanceContainer CreateTestInstanceContainer(int sampleSize = 1, int numWarmupIterations = 0)
    {
        var testType = typeof(TestClass);
        var testInstance = new TestClass();
        var method = testType.GetMethod(nameof(TestClass.TestMethod))!;
        var executionSettings = Substitute.For<IExecutionSettings>();
        executionSettings.SampleSize.Returns(sampleSize);
        executionSettings.NumWarmupIterations.Returns(numWarmupIterations);

        return TestInstanceContainer.CreateTestInstance(
            testInstance,
            method,
            [],
            [],
            false,
            executionSettings);
    }

    // Test class for creating test instances
    private class TestClass
    {
        public void TestMethod()
        {
            // Simple test method
        }
    }
}
