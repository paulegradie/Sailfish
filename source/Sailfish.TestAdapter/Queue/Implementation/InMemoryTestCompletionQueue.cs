using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Configuration;

namespace Sailfish.TestAdapter.Queue.Implementation;

/// <summary>
/// In-memory implementation of the test completion queue using System.Threading.Channels.
/// This implementation provides thread-safe queue operations within the test adapter process
/// and exists only during test execution lifetime without any persistence requirements.
/// </summary>
/// <remarks>
/// The InMemoryTestCompletionQueue is the primary queue implementation for the intercepting
/// queue architecture. It uses a bounded channel to provide high-throughput, thread-safe
/// message queuing for test completion events with configurable capacity limits. The queue
/// supports the full lifecycle of test execution including graceful startup, shutdown, and
/// completion detection.
///
/// Key characteristics:
/// - Thread-safe operations using System.Threading.Channels
/// - In-memory only - no persistence across test runs
/// - Bounded capacity with configurable limits to prevent memory issues
/// - Proper lifecycle management with start/stop/complete operations
/// - Graceful shutdown with completion detection
/// - Comprehensive error handling for invalid operations
/// </remarks>
public class InMemoryTestCompletionQueue : ITestCompletionQueue, IDisposable
{
    private readonly Channel<TestCompletionQueueMessage> _channel;
    private readonly ChannelWriter<TestCompletionQueueMessage> _writer;
    private readonly ChannelReader<TestCompletionQueueMessage> _reader;
    private readonly QueueConfiguration _configuration;

    private volatile bool _isRunning;
    private volatile bool _isCompleted;
    private volatile bool _isDisposed;
    private int _queueDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTestCompletionQueue"/> class
    /// with the specified configuration. Creates a bounded channel with the configured
    /// capacity and stores configuration settings for use by queue consumers.
    /// </summary>
    /// <param name="configuration">
    /// The queue configuration containing capacity, batch processing, and other settings.
    /// Cannot be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configuration"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="configuration.MaxQueueCapacity"/> is less than or equal to 0.
    /// </exception>
    public InMemoryTestCompletionQueue(QueueConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (configuration.MaxQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(configuration),
                "Maximum queue capacity must be greater than 0.");
        }

        _configuration = configuration;

        _channel = Channel.CreateBounded<TestCompletionQueueMessage>(configuration.MaxQueueCapacity);
        _writer = _channel.Writer;
        _reader = _channel.Reader;
    }

    /// <summary>
    /// Gets the queue configuration used to initialize this queue instance.
    /// </summary>
    public QueueConfiguration Configuration => _configuration;

    /// <summary>
    /// Gets the maximum capacity of the queue as configured during initialization.
    /// </summary>
    public int MaxCapacity => _configuration.MaxQueueCapacity;

    /// <inheritdoc />
    public bool IsRunning => _isRunning && !_isDisposed;

    /// <inheritdoc />
    public int QueueDepth
    {
        get
        {
            ThrowIfDisposed();
            return _queueDepth;
        }
    }

    /// <inheritdoc />
    public bool IsCompleted => _isCompleted || _isDisposed;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_isRunning)
        {
            throw new InvalidOperationException("Queue is already running.");
        }

        if (_isCompleted)
        {
            throw new InvalidOperationException("Queue has been completed and cannot be restarted.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CompleteAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        _isCompleted = true;
        _isRunning = false;

        // Complete the writer to signal no more messages will be added
        _writer.Complete();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task EnqueueAsync(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        ThrowIfDisposed();

        if (!_isRunning)
        {
            throw new InvalidOperationException("Queue is not running. Call StartAsync before enqueuing messages.");
        }

        if (_isCompleted)
        {
            throw new InvalidOperationException("Queue has been completed and cannot accept new messages.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _queueDepth);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException("Queue has been completed and cannot accept new messages.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<TestCompletionQueueMessage?> DequeueAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Wait for a message to be available or for the channel to complete
            if (await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_reader.TryRead(out var message))
                {
                    Interlocked.Decrement(ref _queueDepth);
                    return message;
                }
            }

            // Channel is completed and no more messages are available
            return null;
        }
        catch (InvalidOperationException)
        {
            // Channel has been completed
            return null;
        }
    }

    /// <inheritdoc />
    public Task<TestCompletionQueueMessage?> TryDequeueAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        // Try to read a message immediately without waiting
        if (_reader.TryRead(out var message))
        {
            Interlocked.Decrement(ref _queueDepth);
            return Task.FromResult<TestCompletionQueueMessage?>(message);
        }

        // No message available immediately
        return Task.FromResult<TestCompletionQueueMessage?>(null);
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(InMemoryTestCompletionQueue));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="InMemoryTestCompletionQueue"/>.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _isRunning = false;
        _isCompleted = true;

        // Complete the writer to signal no more messages will be added
        _writer.TryComplete();
    }
}
