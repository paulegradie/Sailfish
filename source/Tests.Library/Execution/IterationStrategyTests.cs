using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis;
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
        var reason1 = result.ConvergenceReason!;
        reason1.ShouldContain("5 fixed iterations");
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
        var reason2 = result.ConvergenceReason!;
        reason2.ShouldContain("Converged");
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

    [Fact]
    public async Task AdaptiveIterationStrategy_AdaptiveTuning_LogsCategoryAndUsesTunedThresholds()
    {
        // Arrange: small min to get pilot samples quickly; default targetCV=0.05, MaxCI=0.20
        var strategy = new AdaptiveIterationStrategy(mockLogger, mockConvergenceDetector);
        var container = CreateTestInstanceContainer();
        var settings = CreateExecutionSettings(useAdaptive: true, minSampleSize: 3, maxSampleSize: 10, targetCV: 0.05);

        // Make the first convergence check report converged to keep test fast
        mockConvergenceDetector
            .CheckConvergence(Arg.Any<IReadOnlyList<double>>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>())
            .Returns(ci => new ConvergenceResult { HasConverged = true, CurrentCoefficientOfVariation = 0.02, Reason = "Converged" });

        // Act
        var result = await strategy.ExecuteIterations(container, settings, CancellationToken.None);

        // Assert: we should log the adaptive tuning message at least once
        mockLogger.Received().Log(
            LogLevel.Information,
            "      ---- Adaptive tuning: {Category} -> MinN={MinN}, TargetCV={TargetCV:F3}, MaxCI={MaxCI:F3}",
            Arg.Is<object[]>(vals => vals.Length == 4 && vals[0] is AdaptiveSamplingConfig.SpeedCategory));

        // And the tuned thresholds should be used in the immediate convergence check
        mockConvergenceDetector.Received().CheckConvergence(
            Arg.Any<IReadOnlyList<double>>(),
            Arg.Is<double>(cv => Math.Abs(cv - 0.05) < 1e-6), // CV remains at least the user's 0.05
            Arg.Is<double>(ci => ci <= 0.20 && ci > 0.0), // Tuned MaxCI should be <= default 0.20 (selector-dependent)
            Arg.Is<double>(cl => Math.Abs(cl - 0.95) < 1e-6),
            Arg.Is<int>(min => min >= 50));

        result.IsSuccess.ShouldBeTrue();
    }

        [Fact]
        public async Task FixedIterationStrategy_RespectsTimeBudget_StopsEarly()
        {
            // Arrange: a slow operation (~20ms) and a tight budget (<= 30ms)
            var strategy = new FixedIterationStrategy(mockLogger);
            var instance = new SlowWork();
            var method = typeof(SlowWork).GetMethod(nameof(SlowWork.Run))!;
            var settings = new ExecutionSettings
            {
                SampleSize = 5,
                OperationsPerInvoke = 1,
                MaxMeasurementTimePerMethod = TimeSpan.FromMilliseconds(30)
            };
            var container = TestInstanceContainer.CreateTestInstance(instance, method, [], [], false, settings);

            // Act
            var result = await strategy.ExecuteIterations(container, settings, CancellationToken.None);

            // Assert: stopped early due to time budget
            result.IsSuccess.ShouldBeTrue();
            result.TotalIterations.ShouldBeLessThan(5);
            var reason3 = result.ConvergenceReason!;
            reason3.ShouldContain("Time budget exceeded");
        }

        [Fact]
        public async Task AdaptiveIterationStrategy_RespectsTimeBudgetDuringMinimumPhase_StopsEarly()
        {
            // Arrange: min=3, each iteration ~20ms, budget 25ms so it cannot finish min phase
            var strategy = new AdaptiveIterationStrategy(mockLogger, mockConvergenceDetector);
            var instance = new SlowWork();
            var method = typeof(SlowWork).GetMethod(nameof(SlowWork.Run))!;
            var settings = new ExecutionSettings
            {
                UseAdaptiveSampling = true,
                MinimumSampleSize = 3,
                MaximumSampleSize = 1000,
                TargetCoefficientOfVariation = 0.05,
                ConfidenceLevel = 0.95,
                OperationsPerInvoke = 1,
                MaxMeasurementTimePerMethod = TimeSpan.FromMilliseconds(25)
            };
            var container = TestInstanceContainer.CreateTestInstance(instance, method, [], [], false, settings);

            // Convergence detector won't be used if we stop in minimum phase, but keep a benign default
            mockConvergenceDetector.CheckConvergence(
                Arg.Any<IReadOnlyList<double>>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>())
                .Returns(new ConvergenceResult { HasConverged = false, CurrentCoefficientOfVariation = 0.5, Reason = "N/A" });

            // Act
            var result = await strategy.ExecuteIterations(container, settings, CancellationToken.None);

            // Assert: stopped before reaching the minimum due to time budget
            result.IsSuccess.ShouldBeTrue();
            result.TotalIterations.ShouldBeLessThan(3);
            var reason4 = result.ConvergenceReason!;
            reason4.ShouldContain("Time budget exceeded");
        }

        private sealed class SlowWork
        {
            public async Task Run(CancellationToken ct)
            {
                await Task.Delay(20, ct);
            }
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
            [],
            [],
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
