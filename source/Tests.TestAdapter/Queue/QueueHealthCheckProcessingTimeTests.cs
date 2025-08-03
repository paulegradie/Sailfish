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
/// Tests for QueueHealthCheck processing time tracking functionality.
/// </summary>
public class QueueHealthCheckProcessingTimeTests
{
    private readonly ILogger _logger;
    private readonly QueueConfiguration _configuration;

    public QueueHealthCheckProcessingTimeTests()
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
    public void CalculateAverageProcessingTime_WithNoRecordedTimes_ShouldReturnZero()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        using var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Act
        var result = GetAverageProcessingTime(healthCheck);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void RecordProcessingTime_WithValidDuration_ShouldStoreTime()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        using var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Act
        healthCheck.RecordProcessingTime(100.5);
        var result = GetAverageProcessingTime(healthCheck);

        // Assert
        result.ShouldBe(100.5);
    }

    [Fact]
    public void RecordProcessingTime_WithMultipleDurations_ShouldCalculateCorrectAverage()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        using var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Act
        healthCheck.RecordProcessingTime(100.0);
        healthCheck.RecordProcessingTime(200.0);
        healthCheck.RecordProcessingTime(300.0);
        var result = GetAverageProcessingTime(healthCheck);

        // Assert
        result.ShouldBe(200.0); // (100 + 200 + 300) / 3 = 200
    }

    [Fact]
    public void RecordProcessingTime_WithNegativeDuration_ShouldIgnore()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        using var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Act
        healthCheck.RecordProcessingTime(-50.0);
        var result = GetAverageProcessingTime(healthCheck);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void RecordProcessingTime_WithMixedValidAndInvalidDurations_ShouldOnlyUseValid()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        using var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Act
        healthCheck.RecordProcessingTime(100.0);
        healthCheck.RecordProcessingTime(-50.0); // Should be ignored
        healthCheck.RecordProcessingTime(200.0);
        var result = GetAverageProcessingTime(healthCheck);

        // Assert
        result.ShouldBe(150.0); // (100 + 200) / 2 = 150
    }

    [Fact]
    public async Task GetHealthMetricsAsync_ShouldIncludeAverageProcessingTime()
    {
        // Arrange
        var queueManager = CreateMockQueueManager();
        using var healthCheck = new QueueHealthCheck(queueManager, _configuration, _logger);

        // Record some processing times
        healthCheck.RecordProcessingTime(150.0);
        healthCheck.RecordProcessingTime(250.0);

        // Act
        var metrics = await healthCheck.GetHealthMetricsAsync(CancellationToken.None);

        // Assert
        metrics.AverageProcessingTimeMs.ShouldBe(200.0);
    }

    private TestCompletionQueueManager CreateMockQueueManager()
    {
        var queue = new InMemoryTestCompletionQueue(1000);
        var processors = Array.Empty<ITestCompletionQueueProcessor>();
        return new TestCompletionQueueManager(queue, processors, _logger);
    }

    /// <summary>
    /// Uses reflection to access the private CalculateAverageProcessingTime method for testing.
    /// </summary>
    private double GetAverageProcessingTime(QueueHealthCheck healthCheck)
    {
        var method = typeof(QueueHealthCheck).GetMethod("CalculateAverageProcessingTime", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (double)method!.Invoke(healthCheck, null)!;
    }
}
