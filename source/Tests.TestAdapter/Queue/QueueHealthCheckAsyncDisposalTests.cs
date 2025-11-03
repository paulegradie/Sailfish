using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Implementation;
using Shouldly;
using Xunit;
using ILogger = Sailfish.Logging.ILogger;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Tests for QueueHealthCheck async disposal functionality.
/// </summary>
public class QueueHealthCheckAsyncDisposalTests
{
    private readonly ILogger _logger;
    private readonly QueueConfiguration _configuration;

    public QueueHealthCheckAsyncDisposalTests()
    {
        _logger = Substitute.For<ILogger>();
        _configuration = new QueueConfiguration
        {
            IsEnabled = true,
            ProcessingTimeoutMs = 30000,
            BatchCompletionTimeoutMs = 60000
        };
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeResourcesWithoutDeadlock()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Act
        await healthCheck.DisposeAsync();

        // Assert - Verify that subsequent calls don't throw (idempotent)
        await healthCheck.DisposeAsync();

        // Verify that the object is disposed by checking that operations throw ObjectDisposedException
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            healthCheck.StartAsync(CancellationToken.None));
    }

    [Fact]
    public void Dispose_ShouldNotDeadlock()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Act
        healthCheck.Dispose();

        // Assert - Verify that subsequent calls don't throw (idempotent)
        healthCheck.Dispose();

        // Verify that the object is disposed by checking that operations throw ObjectDisposedException
        Should.Throw<ObjectDisposedException>(() =>
            healthCheck.StartAsync(CancellationToken.None).GetAwaiter().GetResult());
    }

    [Fact]
    public async Task DisposeAsync_AfterSyncDispose_ShouldNotThrow()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Act
        healthCheck.Dispose();
        await healthCheck.DisposeAsync();

        // Assert - Verify that the object is disposed
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            healthCheck.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Dispose_AfterAsyncDispose_ShouldNotThrow()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Act
        await healthCheck.DisposeAsync();
        healthCheck.Dispose();

        // Assert - Verify that the object is disposed
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            healthCheck.StartAsync(CancellationToken.None));
    }

    [Fact]
    public void UsingStatement_ShouldWorkWithoutDeadlock()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        QueueHealthCheck? healthCheck;

        // Act - The using statement will call Dispose() automatically
        using (healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger))
        {
            // This test verifies that it doesn't deadlock
            healthCheck.ShouldNotBeNull();
        }

        // Assert - Verify that the object is disposed after the using block
        Should.Throw<ObjectDisposedException>(() =>
            healthCheck.StartAsync(CancellationToken.None).GetAwaiter().GetResult());
    }

    [Fact]
    public async Task AwaitUsingStatement_ShouldWorkWithoutDeadlock()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        QueueHealthCheck? healthCheck;

        // Act - The await using statement will call DisposeAsync() automatically
        await using (healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger))
        {
            // This test verifies that it doesn't deadlock
            healthCheck.ShouldNotBeNull();
        }

        // Assert - Verify that the object is disposed after the await using block
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            healthCheck.StartAsync(CancellationToken.None));
    }

    private TestCompletionQueueManager CreateMockQueueManager()
    {
        var queue = new InMemoryTestCompletionQueue(1000);
        var processors = Array.Empty<ITestCompletionQueueProcessor>();
        return new TestCompletionQueueManager(queue, processors, _logger);
    }
}
