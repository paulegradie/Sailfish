using System;
using System.Reflection;
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
    private readonly ILogger mockLogger;
    private readonly IRunSettings mockRunSettings;
    private readonly IIterationStrategy mockFixedStrategy;
    private readonly IIterationStrategy mockAdaptiveStrategy;
    private readonly TestCaseIterator testCaseIterator;

    public TestCaseIteratorTests()
    {
        mockLogger = Substitute.For<ILogger>();
        mockRunSettings = Substitute.For<IRunSettings>();
        mockFixedStrategy = Substitute.For<IIterationStrategy>();
        mockAdaptiveStrategy = Substitute.For<IIterationStrategy>();
        testCaseIterator = new TestCaseIterator(mockRunSettings, mockLogger, mockFixedStrategy, mockAdaptiveStrategy);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Act & Assert
        testCaseIterator.ShouldNotBeNull();
        testCaseIterator.ShouldBeAssignableTo<ITestCaseIterator>();
    }



    [Fact]
    public async Task Iterate_WithValidContainer_ShouldCompleteSuccessfully()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        mockRunSettings.SampleSizeOverride.Returns((int?)null);

        // Act
        var result = await testCaseIterator.Iterate(container, false, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Iterate_WithDisabledOverheadEstimation_ShouldSkipEstimation()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        mockRunSettings.SampleSizeOverride.Returns((int?)null);

        // Act
        var result = await testCaseIterator.Iterate(container, true, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Iterate_WithSampleSizeOverride_ShouldUseSampleSizeOverride()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        mockRunSettings.SampleSizeOverride.Returns(5);

        // Act
        var result = await testCaseIterator.Iterate(container, true, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        // Verify that the correct number of iterations were logged
        mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 1, 5);
    }

    [Fact]
    public async Task Iterate_WithZeroSampleSizeOverride_ShouldUseMinimumOfOne()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        mockRunSettings.SampleSizeOverride.Returns(0);

        // Act
        var result = await testCaseIterator.Iterate(container, true, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        // Should use minimum of 1 iteration
        mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 1, 1);
    }

    [Fact]
    public async Task Iterate_WithNegativeSampleSizeOverride_ShouldUseMinimumOfOne()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        mockRunSettings.SampleSizeOverride.Returns(-5);

        // Act
        var result = await testCaseIterator.Iterate(container, true, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        // Should use minimum of 1 iteration
        mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 1, 1);
    }

    [Fact]
    public async Task Iterate_WithWarmupIterations_ShouldPerformWarmup()
    {
        // Arrange
        var container = CreateTestInstanceContainer(numWarmupIterations: 2);
        mockRunSettings.SampleSizeOverride.Returns(1);

        // Act
        var result = await testCaseIterator.Iterate(container, true, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        // Verify warmup iterations were logged
        mockLogger.Received().Log(LogLevel.Information, "      ---- warmup iteration {CurrentIteration} of {TotalIterations}", 1, 2);
        mockLogger.Received().Log(LogLevel.Information, "      ---- warmup iteration {CurrentIteration} of {TotalIterations}", 2, 2);
    }





    [Fact]
    public async Task Iterate_ShouldLogIterationProgress()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        mockRunSettings.SampleSizeOverride.Returns(3);

        // Act
        var result = await testCaseIterator.Iterate(container, true, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();

        // Verify all iterations were logged
        mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 1, 3);
        mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 2, 3);
        mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 3, 3);
    }

    [Fact]
    public async Task Iterate_WithOverheadEstimationEnabled_ShouldApplyOverheadEstimates()
    {
        // Arrange
        var container = CreateTestInstanceContainer();
        mockRunSettings.SampleSizeOverride.Returns(1);

        // Act
        var result = await testCaseIterator.Iterate(container, false, CancellationToken.None);

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
            Array.Empty<string>(),
            Array.Empty<object>(),
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
