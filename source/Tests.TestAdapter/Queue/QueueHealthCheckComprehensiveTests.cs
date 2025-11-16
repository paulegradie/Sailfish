using System;
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
/// Comprehensive unit tests for QueueHealthCheck service.
/// Tests health monitoring, metrics collection, and status evaluation
/// for the queue system infrastructure.
/// </summary>
public class QueueHealthCheckComprehensiveTests : IDisposable
{
    private readonly TestCompletionQueueManager _queueManager;
    private readonly QueueConfiguration _configuration;
    private readonly ILogger _logger;
    private QueueHealthCheck? _healthCheck;

    public QueueHealthCheckComprehensiveTests()
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
        _healthCheck?.Dispose();
        _queueManager?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullQueueManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new QueueHealthCheck(null!, _configuration, _logger));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new QueueHealthCheck(_queueManager, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new QueueHealthCheck(_queueManager, _configuration, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        using var healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);

        // Assert
        healthCheck.ShouldNotBeNull();
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_ShouldStartSuccessfully()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);

        // Act
        await _healthCheck.StartAsync(CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Information, Arg.Is<string>(s => s.Contains("Starting queue health check")));
        _logger.Received().Log(LogLevel.Information, "Queue health check monitoring started with interval of {0} seconds", Arg.Any<object[]>());
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        await _healthCheck.StartAsync(CancellationToken.None);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _healthCheck.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => _healthCheck.StartAsync(cts.Token));
    }

    [Fact]
    public async Task StartAsync_AfterDisposal_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        _healthCheck.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() => _healthCheck.StartAsync(CancellationToken.None));
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_WhenRunning_ShouldStopSuccessfully()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        await _healthCheck.StartAsync(CancellationToken.None);

        // Act
        await _healthCheck.StopAsync(CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Information, Arg.Is<string>(s => s.Contains("Stopping queue health check")));
        _logger.Received().Log(LogLevel.Information, Arg.Is<string>(s => s.Contains("stopped successfully")));
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_ShouldCompleteWithoutError()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);

        // Act & Assert
        await _healthCheck.StopAsync(CancellationToken.None);
        
        // Should complete without throwing
    }

    [Fact]
    public async Task StopAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        await _healthCheck.StartAsync(CancellationToken.None);
        
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => _healthCheck.StopAsync(cts.Token));
    }

    #endregion

    #region GetHealthStatusAsync Tests

    [Fact]
    public async Task GetHealthStatusAsync_WhenHealthy_ShouldReturnHealthyStatus()
    {
        // Arrange
        await _queueManager.StartAsync(_configuration, CancellationToken.None); // Start the queue manager to make it appear healthy
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);

        // Act
        var status = await _healthCheck.GetHealthStatusAsync(CancellationToken.None);

        // Assert
        status.ShouldNotBeNull();
        status.Level.ShouldBe(QueueHealthLevel.Healthy);
        status.Timestamp.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        status.Details.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetHealthStatusAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => _healthCheck.GetHealthStatusAsync(cts.Token));
    }

    [Fact]
    public async Task GetHealthStatusAsync_AfterDisposal_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        _healthCheck.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() => _healthCheck.GetHealthStatusAsync(CancellationToken.None));
    }

    #endregion

    #region GetHealthMetricsAsync Tests

    [Fact]
    public async Task GetHealthMetricsAsync_ShouldReturnValidMetrics()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);

        // Act
        var metrics = await _healthCheck.GetHealthMetricsAsync(CancellationToken.None);

        // Assert
        metrics.ShouldNotBeNull();
        metrics.QueueDepth.ShouldBeGreaterThanOrEqualTo(0);
        metrics.AverageQueueDepth.ShouldBeGreaterThanOrEqualTo(0);
        metrics.PeakQueueDepth.ShouldBeGreaterThanOrEqualTo(0);
        metrics.MessagesProcessedPerSecond.ShouldBeGreaterThanOrEqualTo(0);
        metrics.AverageProcessingTimeMs.ShouldBeGreaterThanOrEqualTo(0);
        metrics.ErrorRate.ShouldBeGreaterThanOrEqualTo(0);
        metrics.SystemUptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        metrics.AdditionalMetrics.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetHealthMetricsAsync_WithProcessingTimes_ShouldCalculateCorrectAverages()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        
        // Record some processing times
        _healthCheck.RecordProcessingTime(100.0);
        _healthCheck.RecordProcessingTime(200.0);
        _healthCheck.RecordProcessingTime(300.0);

        // Act
        var metrics = await _healthCheck.GetHealthMetricsAsync(CancellationToken.None);

        // Assert
        metrics.AverageProcessingTimeMs.ShouldBe(200.0);
    }

    [Fact]
    public async Task GetHealthMetricsAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => _healthCheck.GetHealthMetricsAsync(cts.Token));
    }

    #endregion

    #region Health Status Change Events Tests

    [Fact]
    public async Task HealthStatusChanged_WhenStatusChanges_ShouldRaiseEvent()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        QueueHealthStatusChangedEventArgs? eventArgs = null;
        
        _healthCheck.HealthStatusChanged += (sender, args) => eventArgs = args;

        // Act
        await _healthCheck.StartAsync(CancellationToken.None);
        
        // Wait a bit for the initial health check
        await Task.Delay(100);

        // Assert
        // The initial health check should trigger an event
        eventArgs.ShouldNotBeNull();
        eventArgs.CurrentStatus.ShouldNotBeNull();
        eventArgs.PreviousStatus.ShouldNotBeNull();
        eventArgs.ChangeTimestamp.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_ShouldDisposeCleanly()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);

        // Act & Assert
        _healthCheck.Dispose(); // Should not throw
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeCleanly()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);

        // Act & Assert
        await _healthCheck.DisposeAsync(); // Should not throw
    }

    [Fact]
    public async Task DisposeAsync_WhenRunning_ShouldStopAndDispose()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        await _healthCheck.StartAsync(CancellationToken.None);

        // Act
        await _healthCheck.DisposeAsync();

        // Assert
        // Should dispose without throwing
        _logger.Received().Log(LogLevel.Information, Arg.Is<string>(s => s.Contains("Starting queue health check")));
    }

    #endregion

    #region Exception Tests

    [Fact]
    public async Task StartAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        _healthCheck.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            _healthCheck.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        _healthCheck.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            _healthCheck.StopAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetHealthStatusAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        _healthCheck.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            _healthCheck.GetHealthStatusAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetHealthMetricsAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        _healthCheck.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            _healthCheck.GetHealthMetricsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _healthCheck.StartAsync(cts.Token));
    }

    [Fact]
    public async Task StopAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        await _healthCheck.StartAsync(CancellationToken.None);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _healthCheck.StopAsync(cts.Token));
    }

    [Fact]
    public async Task GetHealthStatusAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        await _healthCheck.StartAsync(CancellationToken.None);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _healthCheck.GetHealthStatusAsync(cts.Token));
    }

    [Fact]
    public async Task GetHealthMetricsAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);
        await _healthCheck.StartAsync(CancellationToken.None);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _healthCheck.GetHealthMetricsAsync(cts.Token));
    }

    [Fact]
    public void RecordProcessingTime_WithNegativeValue_ShouldIgnore()
    {
        // Arrange
        _healthCheck = new QueueHealthCheck(_queueManager, _configuration, _logger);

        // Act - Should not throw
        _healthCheck.RecordProcessingTime(-100);

        // Assert - No exception should be thrown
    }

    #endregion
}
