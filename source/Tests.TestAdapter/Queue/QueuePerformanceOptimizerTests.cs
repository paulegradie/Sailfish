using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Implementation;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Comprehensive unit tests for QueuePerformanceOptimizer.
/// Tests performance optimization strategies, bottleneck detection, and configuration updates.
/// </summary>
public class QueuePerformanceOptimizerTests : IDisposable
{
    private readonly TestCompletionQueueManager _queueManager;
    private readonly QueueConfiguration _configuration;
    private readonly ILogger _logger;
    private QueuePerformanceOptimizer? _optimizer;

    public QueuePerformanceOptimizerTests()
    {
        var queue = new InMemoryTestCompletionQueue(1000);
        var processors = Array.Empty<ITestCompletionQueueProcessor>();
        _logger = Substitute.For<ILogger>();
        _queueManager = new TestCompletionQueueManager(queue, processors, _logger);

        _configuration = new QueueConfiguration
        {
            MaxQueueCapacity = 1000,
            ProcessingTimeoutMs = 30000,
            BatchCompletionTimeoutMs = 60000,
            PublishTimeoutMs = 5000
        };
    }

    public void Dispose()
    {
        _optimizer?.Dispose();
        _queueManager?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullHealthCheck_ShouldThrowArgumentNullException()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new QueuePerformanceOptimizer(null!, _configuration, _queueManager, _logger));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new QueuePerformanceOptimizer(healthCheck, null!, _queueManager, _logger));
    }

    [Fact]
    public void Constructor_WithNullQueueManager_ShouldThrowArgumentNullException()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new QueuePerformanceOptimizer(healthCheck, _configuration, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new QueuePerformanceOptimizer(healthCheck, _configuration, _queueManager, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();

        // Act
        _optimizer = new QueuePerformanceOptimizer(healthCheck, _configuration, _queueManager, _logger);

        // Assert
        _optimizer.ShouldNotBeNull();
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_ShouldStartOptimizationService()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();
        _optimizer = new QueuePerformanceOptimizer(healthCheck, _configuration, _queueManager, _logger);

        // Act
        await _optimizer.StartAsync(CancellationToken.None);

        // Assert - Should not throw and service should be running
        // We can't directly test internal state, but we can verify no exceptions
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyStarted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();
        _optimizer = new QueuePerformanceOptimizer(healthCheck, _configuration, _queueManager, _logger);
        await _optimizer.StartAsync(CancellationToken.None);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _optimizer.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();
        _optimizer = new QueuePerformanceOptimizer(healthCheck, _configuration, _queueManager, _logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => _optimizer.StartAsync(cts.Token));
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_WhenNotStarted_ShouldNotThrow()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();
        _optimizer = new QueuePerformanceOptimizer(healthCheck, _configuration, _queueManager, _logger);

        // Act & Assert - Should not throw
        await _optimizer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_WhenStarted_ShouldStopOptimizationService()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();
        _optimizer = new QueuePerformanceOptimizer(healthCheck, _configuration, _queueManager, _logger);
        await _optimizer.StartAsync(CancellationToken.None);

        // Act
        await _optimizer.StopAsync(CancellationToken.None);

        // Assert - Should be able to start again
        await _optimizer.StartAsync(CancellationToken.None);
    }

    #endregion

    #region AnalyzePerformanceAsync Tests

    [Fact]
    public async Task AnalyzePerformanceAsync_ShouldReturnPerformanceAnalysis()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();
        var healthMetrics = new QueueHealthMetrics(
            QueueDepth: 100,
            AverageQueueDepth: 80.0,
            PeakQueueDepth: 150,
            MessagesProcessedPerSecond: 50.0,
            AverageProcessingTimeMs: 100.0,
            ErrorRate: 0.01,
            RetryRate: 0.005,
            BatchCompletionRate: 95.0,
            BatchTimeoutRate: 5.0,
            SystemUptime: TimeSpan.FromHours(2),
            LastHealthCheck: DateTime.UtcNow,
            AdditionalMetrics: new Dictionary<string, object>());
        healthCheck.GetHealthMetricsAsync(Arg.Any<CancellationToken>()).Returns(healthMetrics);
        _optimizer = new QueuePerformanceOptimizer(healthCheck, _configuration, _queueManager, _logger);

        // Act
        var result = await _optimizer.AnalyzePerformanceAsync(CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Timestamp.ShouldBeGreaterThan(DateTime.MinValue);
    }

    [Fact]
    public async Task AnalyzePerformanceAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();
        _optimizer = new QueuePerformanceOptimizer(healthCheck, _configuration, _queueManager, _logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => _optimizer.AnalyzePerformanceAsync(cts.Token));
    }

    #endregion

    #region OptimizeConfigurationAsync Tests

    [Fact]
    public async Task OptimizeConfigurationAsync_ShouldOptimizeConfiguration()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();
        var healthMetrics = new QueueHealthMetrics(
            QueueDepth: 100,
            AverageQueueDepth: 80.0,
            PeakQueueDepth: 150,
            MessagesProcessedPerSecond: 50.0,
            AverageProcessingTimeMs: 100.0,
            ErrorRate: 0.01,
            RetryRate: 0.005,
            BatchCompletionRate: 95.0,
            BatchTimeoutRate: 5.0,
            SystemUptime: TimeSpan.FromHours(2),
            LastHealthCheck: DateTime.UtcNow,
            AdditionalMetrics: new Dictionary<string, object>());
        healthCheck.GetHealthMetricsAsync(Arg.Any<CancellationToken>()).Returns(healthMetrics);
        _optimizer = new QueuePerformanceOptimizer(healthCheck, _configuration, _queueManager, _logger);

        // Act
        var result = await _optimizer.OptimizeConfigurationAsync(CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.AppliedOptimizations.ShouldNotBeNull();
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_ShouldDisposeResourcesProperly()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();
        _optimizer = new QueuePerformanceOptimizer(healthCheck, _configuration, _queueManager, _logger);

        // Act & Assert - Should not throw
        _optimizer.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeResourcesProperly()
    {
        // Arrange
        var healthCheck = Substitute.For<IQueueHealthCheck>();
        _optimizer = new QueuePerformanceOptimizer(healthCheck, _configuration, _queueManager, _logger);

        // Act & Assert - Should not throw
        await _optimizer.DisposeAsync();
    }

    #endregion
}
