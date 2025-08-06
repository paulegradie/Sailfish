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
/// - Configuration-aware with support for queue settings
///
/// Configuration Support:
/// The queue accepts a QueueConfiguration object that controls various aspects of its behavior:
/// - MaxQueueCapacity: Applied to the underlying channel capacity
/// - EnableBatchProcessing, MaxBatchSize: Stored for future use by queue consumers
/// - Other settings: Available via Configuration property for processors and consumers
///
/// The implementation is designed to be lightweight and efficient for the test adapter
/// runtime environment where queue operations must not impact test execution performance.
/// </remarks>
public class InMemoryTestCompletionQueue : ITestCompletionQueue, IDisposable
{
    private readonly Channel<TestCompletionQueueMessage> _channel;
    private readonly ChannelWriter<TestCompletionQueueMessage> _writer;
    private readonly ChannelReader<TestCompletionQueueMessage> _reader;
    private readonly int _maxCapacity;
    private readonly QueueConfiguration? _configuration;

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
        _maxCapacity = configuration.MaxQueueCapacity;

        // Create a bounded channel with the configured capacity
        _channel = Channel.CreateBounded<TestCompletionQueueMessage>(_maxCapacity);
        _writer = _channel.Writer;
        _reader = _channel.Reader;

        _isRunning = false;
        _isCompleted = false;
        _isDisposed = false;
        _queueDepth = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTestCompletionQueue"/> class.
    /// Creates a bounded channel with the specified capacity for controlled memory usage.
    /// </summary>
    /// <param name="maxCapacity">
    /// The maximum number of messages that can be queued. Must be greater than 0.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxCapacity"/> is less than or equal to 0.
    /// </exception>
    /// <remarks>
    /// This constructor is provided for backward compatibility. For full configuration support,
    /// use the constructor that accepts a <see cref="QueueConfiguration"/> parameter.
    /// </remarks>
    public InMemoryTestCompletionQueue(int maxCapacity)
    {
        if (maxCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCapacity),
                "Maximum capacity must be greater than 0.");
        }

        _configuration = null;
        _maxCapacity = maxCapacity;

        // Create a bounded channel with the specified capacity
        _channel = Channel.CreateBounded<TestCompletionQueueMessage>(maxCapacity);
        _writer = _channel.Writer;
        _reader = _channel.Reader;

        _isRunning = false;
        _isCompleted = false;
        _isDisposed = false;
        _queueDepth = 0;
    }

    /// <summary>
    /// Gets the queue configuration used to initialize this queue instance.
    /// Returns null if the queue was created using the legacy constructor.
    /// </summary>
    /// <value>
    /// The <see cref="QueueConfiguration"/> used to create this queue, or null if created
    /// with the legacy constructor that only accepts capacity.
    /// </value>
    /// <remarks>
    /// This property provides access to the full configuration for queue consumers and processors
    /// that need to access batch processing settings, timeouts, and other configuration values.
    /// When null, consumers should use appropriate default values.
    /// </remarks>
    public QueueConfiguration? Configuration => _configuration;

    /// <summary>
    /// Gets the maximum capacity of the queue as configured during initialization.
    /// </summary>
    /// <value>
    /// The maximum number of messages that can be queued simultaneously.
    /// </value>
    public int MaxCapacity => _maxCapacity;

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

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the queue has been disposed.
    /// </summary>
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
