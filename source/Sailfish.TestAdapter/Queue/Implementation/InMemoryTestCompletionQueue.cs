using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sailfish.TestAdapter.Queue.Contracts;

namespace Sailfish.TestAdapter.Queue.Implementation;

/// <summary>
/// In-memory implementation of the test completion queue using System.Threading.Channels.
/// This implementation provides thread-safe queue operations within the test adapter process
/// and exists only during test execution lifetime without any persistence requirements.
/// </summary>
/// <remarks>
/// The InMemoryTestCompletionQueue is the primary queue implementation for the intercepting
/// queue architecture. It uses an unbounded channel to provide high-throughput, thread-safe
/// message queuing for test completion events. The queue supports the full lifecycle of
/// test execution including graceful startup, shutdown, and completion detection.
/// 
/// Key characteristics:
/// - Thread-safe operations using System.Threading.Channels
/// - In-memory only - no persistence across test runs
/// - Unbounded capacity to handle high-throughput test scenarios
/// - Proper lifecycle management with start/stop/complete operations
/// - Graceful shutdown with completion detection
/// - Comprehensive error handling for invalid operations
/// 
/// The implementation is designed to be lightweight and efficient for the test adapter
/// runtime environment where queue operations must not impact test execution performance.
/// </remarks>
public class InMemoryTestCompletionQueue : ITestCompletionQueue, IDisposable
{
    private readonly Channel<TestCompletionQueueMessage> _channel;
    private readonly ChannelWriter<TestCompletionQueueMessage> _writer;
    private readonly ChannelReader<TestCompletionQueueMessage> _reader;
    
    private volatile bool _isRunning;
    private volatile bool _isCompleted;
    private volatile bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTestCompletionQueue"/> class.
    /// Creates an unbounded channel for high-throughput message processing.
    /// </summary>
    public InMemoryTestCompletionQueue()
    {
        // Create an unbounded channel for high-throughput scenarios
        _channel = Channel.CreateUnbounded<TestCompletionQueueMessage>();
        _writer = _channel.Writer;
        _reader = _channel.Reader;
        
        _isRunning = false;
        _isCompleted = false;
        _isDisposed = false;
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning && !_isDisposed;

    /// <inheritdoc />
    public int QueueDepth
    {
        get
        {
            ThrowIfDisposed();
            // For unbounded channels, we can't get exact count efficiently without consuming messages
            // Return 0 if completed or not running, otherwise we can't determine depth without side effects
            return _isCompleted || !_isRunning ? 0 : 0;
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
