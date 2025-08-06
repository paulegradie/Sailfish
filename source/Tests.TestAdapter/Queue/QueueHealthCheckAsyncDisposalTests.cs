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

        // Act & Assert - This should not deadlock
        await healthCheck.DisposeAsync();
        
        // Verify that subsequent calls don't throw
        await healthCheck.DisposeAsync();
    }

    [Fact]
    public void Dispose_ShouldNotDeadlock()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Act & Assert - This should not deadlock
        healthCheck.Dispose();
        
        // Verify that subsequent calls don't throw
        healthCheck.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_AfterSyncDispose_ShouldNotThrow()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Act
        healthCheck.Dispose();
        
        // Assert - Should not throw
        await healthCheck.DisposeAsync();
    }

    [Fact]
    public async Task Dispose_AfterAsyncDispose_ShouldNotThrow()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Act
        await healthCheck.DisposeAsync();
        
        // Assert - Should not throw
        healthCheck.Dispose();
    }

    [Fact]
    public async Task UsingStatement_ShouldWorkWithoutDeadlock()
    {
        // Arrange & Act & Assert - This should not deadlock
        var queueManager = CreateMockQueueManager();
        using var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);
        
        // The using statement will call Dispose() automatically
        // This test verifies that it doesn't deadlock
    }

    [Fact]
    public async Task AwaitUsingStatement_ShouldWorkWithoutDeadlock()
    {
        // Arrange & Act & Assert - This should not deadlock
        var queueManager = CreateMockQueueManager();
        await using var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);
        
        // The await using statement will call DisposeAsync() automatically
        // This test verifies that it doesn't deadlock
    }

    private TestCompletionQueueManager CreateMockQueueManager()
    {
        var queue = new InMemoryTestCompletionQueue(1000);
        var processors = Array.Empty<ITestCompletionQueueProcessor>();
        return new TestCompletionQueueManager(queue, processors, _logger);
    }
}
