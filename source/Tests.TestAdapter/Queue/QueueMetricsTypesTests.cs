using System;
using System.Collections.Generic;
using System.Text.Json;
using Sailfish.TestAdapter.Queue.Contracts;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Comprehensive unit tests for QueueMetricsTypes record types.
/// Tests serialization, equality, and data integrity for queue metrics data structures.
/// </summary>
public class QueueMetricsTypesTests
{
    #region QueueMetricsSnapshot Tests

    [Fact]
    public void QueueMetricsSnapshot_Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-5);
        var endTime = DateTime.UtcNow;
        var queueDepthStats = new QueueDepthStats(10.5, 20, 5, "stable");
        var batchStats = new BatchStats(100, 95, 5, 95.0, 5.0);
        var processorStats = new Dictionary<string, ProcessorStats>
        {
            ["Processor1"] = new ProcessorStats(50, 2, 100.5, 4.0)
        };

        // Act
        var snapshot = new QueueMetricsSnapshot(
            startTime,
            endTime,
            1000,
            950,
            50,
            125.5,
            15.8,
            5.26,
            queueDepthStats,
            batchStats,
            processorStats);

        // Assert
        snapshot.StartTime.ShouldBe(startTime);
        snapshot.EndTime.ShouldBe(endTime);
        snapshot.MessagesPublished.ShouldBe(1000);
        snapshot.MessagesProcessed.ShouldBe(950);
        snapshot.MessagesFailed.ShouldBe(50);
        snapshot.AverageProcessingTimeMs.ShouldBe(125.5);
        snapshot.ProcessingRatePerSecond.ShouldBe(15.8);
        snapshot.ErrorRate.ShouldBe(5.26);
        snapshot.QueueDepthStats.ShouldBe(queueDepthStats);
        snapshot.BatchStats.ShouldBe(batchStats);
        snapshot.ProcessorStats.ShouldBe(processorStats);
    }

    [Fact]
    public void QueueMetricsSnapshot_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow.AddMinutes(5);
        var queueDepthStats = new QueueDepthStats(10.5, 20, 5, "stable");
        var batchStats = new BatchStats(100, 95, 5, 95.0, 5.0);
        var processorStats = new Dictionary<string, ProcessorStats>();

        var snapshot1 = new QueueMetricsSnapshot(startTime, endTime, 1000, 950, 50, 125.5, 15.8, 5.26, queueDepthStats, batchStats, processorStats);
        var snapshot2 = new QueueMetricsSnapshot(startTime, endTime, 1000, 950, 50, 125.5, 15.8, 5.26, queueDepthStats, batchStats, processorStats);

        // Act & Assert
        snapshot1.ShouldBe(snapshot2);
        snapshot1.GetHashCode().ShouldBe(snapshot2.GetHashCode());
    }

    [Fact]
    public void QueueMetricsSnapshot_Serialization_ShouldWorkCorrectly()
    {
        // Arrange
        var snapshot = new QueueMetricsSnapshot(
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(5),
            1000,
            950,
            50,
            125.5,
            15.8,
            5.26,
            new QueueDepthStats(10.5, 20, 5, "stable"),
            new BatchStats(100, 95, 5, 95.0, 5.0),
            new Dictionary<string, ProcessorStats>());

        // Act
        var json = JsonSerializer.Serialize(snapshot);
        var deserialized = JsonSerializer.Deserialize<QueueMetricsSnapshot>(json);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.MessagesPublished.ShouldBe(snapshot.MessagesPublished);
        deserialized.MessagesProcessed.ShouldBe(snapshot.MessagesProcessed);
        deserialized.MessagesFailed.ShouldBe(snapshot.MessagesFailed);
        deserialized.AverageProcessingTimeMs.ShouldBe(snapshot.AverageProcessingTimeMs);
        deserialized.ProcessingRatePerSecond.ShouldBe(snapshot.ProcessingRatePerSecond);
        deserialized.ErrorRate.ShouldBe(snapshot.ErrorRate);
    }

    #endregion

    #region ProcessingRateMetrics Tests

    [Fact]
    public void ProcessingRateMetrics_Constructor_ShouldSetAllProperties()
    {
        // Act
        var metrics = new ProcessingRateMetrics(
            10.5,
            8.2,
            15.7,
            125.5,
            110.3,
            200.8,
            "increasing",
            "stable");

        // Assert
        metrics.CurrentRatePerSecond.ShouldBe(10.5);
        metrics.AverageRatePerSecond.ShouldBe(8.2);
        metrics.PeakRatePerSecond.ShouldBe(15.7);
        metrics.CurrentLatencyMs.ShouldBe(125.5);
        metrics.AverageLatencyMs.ShouldBe(110.3);
        metrics.PeakLatencyMs.ShouldBe(200.8);
        metrics.ThroughputTrend.ShouldBe("increasing");
        metrics.LatencyTrend.ShouldBe("stable");
    }

    [Fact]
    public void ProcessingRateMetrics_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var metrics1 = new ProcessingRateMetrics(10.5, 8.2, 15.7, 125.5, 110.3, 200.8, "increasing", "stable");
        var metrics2 = new ProcessingRateMetrics(10.5, 8.2, 15.7, 125.5, 110.3, 200.8, "increasing", "stable");

        // Act & Assert
        metrics1.ShouldBe(metrics2);
        metrics1.GetHashCode().ShouldBe(metrics2.GetHashCode());
    }

    #endregion

    #region QueueDepthMetrics Tests

    [Fact]
    public void QueueDepthMetrics_Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var depthHistory = new List<QueueDepthMeasurement>
        {
            new(DateTime.UtcNow.AddMinutes(-2), 5),
            new(DateTime.UtcNow.AddMinutes(-1), 10),
            new(DateTime.UtcNow, 8)
        };

        // Act
        var metrics = new QueueDepthMetrics(8, 7.5, 15, 2, "stable", depthHistory);

        // Assert
        metrics.CurrentDepth.ShouldBe(8);
        metrics.AverageDepth.ShouldBe(7.5);
        metrics.PeakDepth.ShouldBe(15);
        metrics.MinimumDepth.ShouldBe(2);
        metrics.DepthTrend.ShouldBe("stable");
        metrics.DepthHistory.ShouldBe(depthHistory);
        metrics.DepthHistory.Count.ShouldBe(3);
    }

    #endregion

    #region BatchMetrics Tests

    [Fact]
    public void BatchMetrics_Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var batchSizeDistribution = new Dictionary<int, int>
        {
            [10] = 5,
            [20] = 8,
            [30] = 3
        };

        // Act
        var metrics = new BatchMetrics(100, 95, 5, 95.0, 5.0, 18.5, 2500.0, batchSizeDistribution);

        // Assert
        metrics.TotalBatches.ShouldBe(100);
        metrics.CompletedBatches.ShouldBe(95);
        metrics.TimedOutBatches.ShouldBe(5);
        metrics.CompletionRate.ShouldBe(95.0);
        metrics.TimeoutRate.ShouldBe(5.0);
        metrics.AverageBatchSize.ShouldBe(18.5);
        metrics.AverageBatchProcessingTimeMs.ShouldBe(2500.0);
        metrics.BatchSizeDistribution.ShouldBe(batchSizeDistribution);
    }

    #endregion

    #region QueueDepthStats Tests

    [Fact]
    public void QueueDepthStats_Constructor_ShouldSetAllProperties()
    {
        // Act
        var stats = new QueueDepthStats(12.5, 25, 3, "increasing");

        // Assert
        stats.Average.ShouldBe(12.5);
        stats.Peak.ShouldBe(25);
        stats.Minimum.ShouldBe(3);
        stats.Trend.ShouldBe("increasing");
    }

    [Fact]
    public void QueueDepthStats_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var stats1 = new QueueDepthStats(12.5, 25, 3, "increasing");
        var stats2 = new QueueDepthStats(12.5, 25, 3, "increasing");

        // Act & Assert
        stats1.ShouldBe(stats2);
        stats1.GetHashCode().ShouldBe(stats2.GetHashCode());
    }

    #endregion

    #region BatchStats Tests

    [Fact]
    public void BatchStats_Constructor_ShouldSetAllProperties()
    {
        // Act
        var stats = new BatchStats(150, 140, 10, 93.33, 6.67);

        // Assert
        stats.Total.ShouldBe(150);
        stats.Completed.ShouldBe(140);
        stats.TimedOut.ShouldBe(10);
        stats.CompletionRate.ShouldBe(93.33);
        stats.TimeoutRate.ShouldBe(6.67);
    }

    [Fact]
    public void BatchStats_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var stats1 = new BatchStats(150, 140, 10, 93.33, 6.67);
        var stats2 = new BatchStats(150, 140, 10, 93.33, 6.67);

        // Act & Assert
        stats1.ShouldBe(stats2);
        stats1.GetHashCode().ShouldBe(stats2.GetHashCode());
    }

    #endregion

    #region ProcessorStats Tests

    [Fact]
    public void ProcessorStats_Constructor_ShouldSetAllProperties()
    {
        // Act
        var stats = new ProcessorStats(1000, 25, 150.5, 2.5);

        // Assert
        stats.Processed.ShouldBe(1000);
        stats.Failed.ShouldBe(25);
        stats.AverageTimeMs.ShouldBe(150.5);
        stats.ErrorRate.ShouldBe(2.5);
    }

    [Fact]
    public void ProcessorStats_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var stats1 = new ProcessorStats(1000, 25, 150.5, 2.5);
        var stats2 = new ProcessorStats(1000, 25, 150.5, 2.5);

        // Act & Assert
        stats1.ShouldBe(stats2);
        stats1.GetHashCode().ShouldBe(stats2.GetHashCode());
    }

    #endregion

    #region QueueDepthMeasurement Tests

    [Fact]
    public void QueueDepthMeasurement_Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var measurement = new QueueDepthMeasurement(timestamp, 15);

        // Assert
        measurement.Timestamp.ShouldBe(timestamp);
        measurement.Depth.ShouldBe(15);
    }

    [Fact]
    public void QueueDepthMeasurement_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var measurement1 = new QueueDepthMeasurement(timestamp, 15);
        var measurement2 = new QueueDepthMeasurement(timestamp, 15);

        // Act & Assert
        measurement1.ShouldBe(measurement2);
        measurement1.GetHashCode().ShouldBe(measurement2.GetHashCode());
    }

    [Fact]
    public void QueueDepthMeasurement_Serialization_ShouldWorkCorrectly()
    {
        // Arrange
        var measurement = new QueueDepthMeasurement(DateTime.UtcNow, 15);

        // Act
        var json = JsonSerializer.Serialize(measurement);
        var deserialized = JsonSerializer.Deserialize<QueueDepthMeasurement>(json);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.Depth.ShouldBe(measurement.Depth);
        deserialized.Timestamp.ShouldBeInRange(
            measurement.Timestamp.AddMilliseconds(-1), 
            measurement.Timestamp.AddMilliseconds(1));
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void ComplexMetricsScenario_ShouldHandleAllTypesCorrectly()
    {
        // Arrange
        var depthHistory = new List<QueueDepthMeasurement>
        {
            new(DateTime.UtcNow.AddMinutes(-5), 0),
            new(DateTime.UtcNow.AddMinutes(-3), 10),
            new(DateTime.UtcNow.AddMinutes(-1), 5),
            new(DateTime.UtcNow, 2)
        };

        var processorStats = new Dictionary<string, ProcessorStats>
        {
            ["FrameworkPublisher"] = new ProcessorStats(500, 5, 50.0, 1.0),
            ["LoggingProcessor"] = new ProcessorStats(500, 0, 25.0, 0.0),
            ["ComparisonProcessor"] = new ProcessorStats(100, 2, 200.0, 2.0)
        };

        var batchSizeDistribution = new Dictionary<int, int>
        {
            [1] = 10,
            [5] = 15,
            [10] = 20,
            [25] = 8,
            [50] = 2
        };

        // Act
        var snapshot = new QueueMetricsSnapshot(
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            1000,
            950,
            50,
            75.5,
            15.8,
            5.26,
            new QueueDepthStats(4.25, 10, 0, "decreasing"),
            new BatchStats(55, 53, 2, 96.36, 3.64),
            processorStats);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ProcessorStats.Count.ShouldBe(3);
        snapshot.ProcessorStats["FrameworkPublisher"].Processed.ShouldBe(500);
        snapshot.ProcessorStats["LoggingProcessor"].ErrorRate.ShouldBe(0.0);
        snapshot.ProcessorStats["ComparisonProcessor"].AverageTimeMs.ShouldBe(200.0);
        
        snapshot.QueueDepthStats.Trend.ShouldBe("decreasing");
        snapshot.BatchStats.CompletionRate.ShouldBe(96.36);
    }

    #endregion
}
