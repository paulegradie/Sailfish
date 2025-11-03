using System;
using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Logging;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

/// <summary>
/// Comprehensive unit tests for iteration strategies.
/// Tests both FixedIterationStrategy and AdaptiveIterationStrategy behavior.
/// </summary>
public class IterationStrategyTests
{
    private readonly ILogger mockLogger;
    private readonly IStatisticalConvergenceDetector mockConvergenceDetector;

    public IterationStrategyTests()
    {
        mockLogger = Substitute.For<ILogger>();
        mockConvergenceDetector = Substitute.For<IStatisticalConvergenceDetector>();
    }

    #region FixedIterationStrategy Tests

    [Fact]
    public void FixedIterationStrategy_Constructor_WithValidLogger_ShouldCreateInstance()
    {
        // Act
        var strategy = new FixedIterationStrategy(mockLogger);

        // Assert
        strategy.ShouldNotBeNull();
        strategy.ShouldBeAssignableTo<IIterationStrategy>();
    }

    [Fact]
    public async Task FixedIterationStrategy_ExecuteIterations_WithValidSettings_ShouldCompleteAllIterations()
    {
        // Arrange
        var strategy = new FixedIterationStrategy(mockLogger);
        var container = CreateTestInstanceContainer();
        var settings = CreateExecutionSettings(sampleSize: 5);

        // Act
        var result = await strategy.ExecuteIterations(container, settings, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.TotalIterations.ShouldBe(5);
        result.ConvergedEarly.ShouldBeFalse();
        result.ConvergenceReason.ShouldContain("5 fixed iterations");
    }

    [Fact]
    public async Task FixedIterationStrategy_ExecuteIterations_ShouldLogIterationProgress()
    {
        // Arrange
        var strategy = new FixedIterationStrategy(mockLogger);
        var container = CreateTestInstanceContainer();
        var settings = CreateExecutionSettings(sampleSize: 3);

        // Act
        await strategy.ExecuteIterations(container, settings, CancellationToken.None);

        // Assert
        mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 1, 3);
        mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 2, 3);
        mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", 3, 3);
    }

    #endregion

    #region AdaptiveIterationStrategy Tests

    [Fact]
    public void AdaptiveIterationStrategy_Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Act
        var strategy = new AdaptiveIterationStrategy(mockLogger, mockConvergenceDetector);

        // Assert
        strategy.ShouldNotBeNull();
        strategy.ShouldBeAssignableTo<IIterationStrategy>();
    }

    [Fact]
    public async Task AdaptiveIterationStrategy_ExecuteIterations_WithEarlyConvergence_ShouldStopEarly()
    {
        // Arrange
        var strategy = new AdaptiveIterationStrategy(mockLogger, mockConvergenceDetector);
        var container = CreateTestInstanceContainer();
        var settings = CreateExecutionSettings(
            useAdaptive: true,
            minSampleSize: 5,
            maxSampleSize: 100,
            targetCV: 0.05);

        // Setup convergence detector to return converged after minimum samples
        mockConvergenceDetector.CheckConvergence(Arg.Any<IReadOnlyList<double>>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>())
            .Returns(new ConvergenceResult
            {
                HasConverged = true,
                CurrentCoefficientOfVariation = 0.03,
                Reason = "Converged: CV 0.03 <= target 0.05"
            });

        // Act
        var result = await strategy.ExecuteIterations(container, settings, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.TotalIterations.ShouldBe(5); // Should stop at minimum
        result.ConvergedEarly.ShouldBeTrue();
        result.ConvergenceReason.ShouldContain("Converged");
    }

    [Fact]
    public async Task AdaptiveIterationStrategy_ExecuteIterations_WithoutConvergence_ShouldReachMaximum()
    {
        // Arrange
        var strategy = new AdaptiveIterationStrategy(mockLogger, mockConvergenceDetector);
        var container = CreateTestInstanceContainer();
        var settings = CreateExecutionSettings(
            useAdaptive: true,
            minSampleSize: 5,
            maxSampleSize: 10,
            targetCV: 0.05);

        // Setup convergence detector to never converge
        mockConvergenceDetector.CheckConvergence(Arg.Any<IReadOnlyList<double>>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>())
            .Returns(new ConvergenceResult
            {
                HasConverged = false,
                CurrentCoefficientOfVariation = 0.15,
                Reason = "Not converged: CV 0.15 > target 0.05"
            });

        // Act
        var result = await strategy.ExecuteIterations(container, settings, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.TotalIterations.ShouldBe(10); // Should reach maximum
        result.ConvergedEarly.ShouldBeFalse();
    }

    [Fact]
    public async Task AdaptiveIterationStrategy_ExecuteIterations_ShouldLogMinimumPhase()
    {
        // Arrange
        var strategy = new AdaptiveIterationStrategy(mockLogger, mockConvergenceDetector);
        var container = CreateTestInstanceContainer();
        var settings = CreateExecutionSettings(
            useAdaptive: true,
            minSampleSize: 3,
            maxSampleSize: 10);

        mockConvergenceDetector.CheckConvergence(Arg.Any<IReadOnlyList<double>>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>())
            .Returns(new ConvergenceResult { HasConverged = true });

        // Act
        await strategy.ExecuteIterations(container, settings, CancellationToken.None);

        // Assert
        mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} (minimum phase)", 1);
        mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} (minimum phase)", 2);
        mockLogger.Received().Log(LogLevel.Information, "      ---- iteration {CurrentIteration} (minimum phase)", 3);
    }

    #endregion

    #region Helper Methods

    private TestInstanceContainer CreateTestInstanceContainer()
    {
        var mockCoreInvoker = Substitute.For<CoreInvoker>(new object(), typeof(object).GetMethod("ToString")!, new PerformanceTimer());
        var mockExecutionSettings = Substitute.For<IExecutionSettings>();

        return TestInstanceContainer.CreateTestInstance(
            new object(),
            typeof(object).GetMethod("ToString")!,
            Array.Empty<string>(),
            Array.Empty<object>(),
            false,
            mockExecutionSettings);
    }

    private IExecutionSettings CreateExecutionSettings(
        int sampleSize = 10,
        bool useAdaptive = false,
        int minSampleSize = 10,
        int maxSampleSize = 1000,
        double targetCV = 0.05)
    {
        return new ExecutionSettings
        {
            SampleSize = sampleSize,
            UseAdaptiveSampling = useAdaptive,
            MinimumSampleSize = minSampleSize,
            MaximumSampleSize = maxSampleSize,
            TargetCoefficientOfVariation = targetCV,
            ConfidenceLevel = 0.95
        };
    }

    #endregion
}
