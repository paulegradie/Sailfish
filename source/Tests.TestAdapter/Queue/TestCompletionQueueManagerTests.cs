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
/// Comprehensive unit tests for TestCompletionQueueManager.
/// Tests queue management, processor registration, lifecycle operations, and error handling.
/// </summary>
public class TestCompletionQueueManagerTests : IDisposable
{
    private readonly ITestCompletionQueue _queue;
    private readonly ITestCompletionQueueProcessor[] _processors;
    private readonly ILogger _logger;
    private TestCompletionQueueManager? _manager;

    public TestCompletionQueueManagerTests()
    {
        _queue = Substitute.For<ITestCompletionQueue>();
        _processors = new[] { Substitute.For<ITestCompletionQueueProcessor>() };
        _logger = Substitute.For<ILogger>();
    }

    public void Dispose()
    {
        _manager?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullQueue_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionQueueManager(null!, _processors, _logger));
    }

    [Fact]
    public void Constructor_WithNullProcessors_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionQueueManager(_queue, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionQueueManager(_queue, _processors, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);

        // Assert
        _manager.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithEmptyProcessors_ShouldCreateInstance()
    {
        // Arrange
        var emptyProcessors = Array.Empty<ITestCompletionQueueProcessor>();

        // Act
        _manager = new TestCompletionQueueManager(_queue, emptyProcessors, _logger);

        // Assert
        _manager.ShouldNotBeNull();
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _manager.StartAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WithValidConfiguration_ShouldStartSuccessfully()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };

        // Act
        await _manager.StartAsync(configuration, CancellationToken.None);

        // Assert
        _manager.IsRunning.ShouldBeTrue();
        _logger.Received(1).Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Test completion queue manager started successfully")));
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };
        await _manager.StartAsync(configuration, CancellationToken.None);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _manager.StartAsync(configuration, CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_ShouldStartQueueAndConsumer()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };

        // Act
        await _manager.StartAsync(configuration, CancellationToken.None);

        // Assert
        await _queue.Received(1).StartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => _manager.StartAsync(configuration, cts.Token));
    }

    [Fact]
    public async Task StartAsync_WhenQueueStartFails_ShouldThrowException()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };
        _queue.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("Queue start failed")));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _manager.StartAsync(configuration, CancellationToken.None));
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_WhenNotRunning_ShouldNotThrow()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);

        // Act
        await _manager.StopAsync(CancellationToken.None);

        // Assert
        _manager.IsRunning.ShouldBeFalse();
        // When not running, StopAsync returns early without logging anything
        _logger.DidNotReceive().Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Stopping test completion queue manager")));
    }

    [Fact]
    public async Task StopAsync_WhenRunning_ShouldStopQueueAndConsumer()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };
        await _manager.StartAsync(configuration, CancellationToken.None);

        // Act
        await _manager.StopAsync(CancellationToken.None);

        // Assert
        await _queue.Received(1).StopAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };
        await _manager.StartAsync(configuration, CancellationToken.None);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => _manager.StopAsync(cts.Token));
    }

    #endregion

    #region CompleteAsync Tests

    [Fact]
    public async Task CompleteAsync_WhenNotRunning_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _manager.CompleteAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CompleteAsync_WhenRunning_ShouldCompleteQueue()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };
        await _manager.StartAsync(configuration, CancellationToken.None);

        // Act
        await _manager.CompleteAsync(CancellationToken.None);

        // Assert
        await _queue.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };
        await _manager.StartAsync(configuration, CancellationToken.None);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => _manager.CompleteAsync(cts.Token));
    }

    #endregion

    // GetQueueDepth method doesn't exist on TestCompletionQueueManager

    #region IsRunning Tests

    [Fact]
    public void IsRunning_WhenNotStarted_ShouldReturnFalse()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);

        // Act
        var isRunning = _manager.IsRunning;

        // Assert
        isRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task IsRunning_WhenStarted_ShouldReturnTrue()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };

        // Act
        await _manager.StartAsync(configuration, CancellationToken.None);

        // Assert
        _manager.IsRunning.ShouldBeTrue();
    }

    [Fact]
    public async Task IsRunning_AfterStop_ShouldReturnFalse()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };
        await _manager.StartAsync(configuration, CancellationToken.None);

        // Act
        await _manager.StopAsync(CancellationToken.None);

        // Assert
        _manager.IsRunning.ShouldBeFalse();
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_ShouldDisposeResourcesProperly()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);

        // Act
        _manager.Dispose();

        // Assert
        _logger.Received(1).Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Test completion queue manager disposed successfully")));
    }

    // TestCompletionQueueManager doesn't implement IAsyncDisposable

    [Fact]
    public async Task Dispose_WhenRunning_ShouldStopFirst()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };
        await _manager.StartAsync(configuration, CancellationToken.None);

        // Act
        _manager.Dispose();

        // Assert
        _manager.IsRunning.ShouldBeFalse();
    }

    #endregion

    #region Exception Tests

    [Fact]
    public async Task StartAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };
        _manager.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            _manager.StartAsync(configuration, CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        _manager.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            _manager.StopAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CompleteAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        _manager.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            _manager.CompleteAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            _manager.StopAsync(CancellationToken.None));
        exception.Message.ShouldContain("not running");
    }

    [Fact]
    public async Task StartAsync_WhenQueueStartFails_ShouldLogErrorAndRethrow()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };
        var exception = new InvalidOperationException("Queue start failed");
        _queue.StartAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(exception));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _manager.StartAsync(configuration, CancellationToken.None));

        _logger.Received().Log(LogLevel.Error, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task StopAsync_WhenQueueStopFails_ShouldLogErrorAndRethrow()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);
        var configuration = new QueueConfiguration { IsEnabled = true };
        await _manager.StartAsync(configuration, CancellationToken.None);

        var exception = new InvalidOperationException("Queue stop failed");
        _queue.StopAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(exception));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _manager.StopAsync(CancellationToken.None));

        _logger.Received().Log(LogLevel.Error, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public void Dispose_MultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        _manager = new TestCompletionQueueManager(_queue, _processors, _logger);

        // Act & Assert - Should not throw
        _manager.Dispose();
        _manager.Dispose();
        _manager.Dispose();
    }

    #endregion
}
