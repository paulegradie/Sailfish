using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Represents a message containing test completion data for queue processing.
/// This message is published when a test case completes execution and contains
/// all relevant information for asynchronous processing by queue processors.
/// </summary>
public class TestCompletionQueueMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the test case.
    /// This corresponds to the TestCaseId.DisplayName from the test execution.
    /// </summary>
    [JsonPropertyName("testCaseId")]
    public string TestCaseId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the test execution result information.
    /// Contains success/failure status and any exception details.
    /// </summary>
    [JsonPropertyName("testResult")]
    public TestExecutionResult TestResult { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the test case completed execution.
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata associated with the test execution.
    /// This can include test class information, execution settings, and other contextual data.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the performance metrics collected during test execution.
    /// Contains timing information, statistical data, and performance characteristics.
    /// </summary>
    [JsonPropertyName("performanceMetrics")]
    public PerformanceMetrics PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Represents the execution result of a test case.
/// </summary>
public class TestExecutionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the test execution was successful.
    /// </summary>
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the exception message if the test failed.
    /// Null if the test was successful.
    /// </summary>
    [JsonPropertyName("exceptionMessage")]
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// Gets or sets the full exception details if the test failed.
    /// Null if the test was successful.
    /// </summary>
    [JsonPropertyName("exceptionDetails")]
    public string? ExceptionDetails { get; set; }

    /// <summary>
    /// Gets or sets the exception type name if the test failed.
    /// Null if the test was successful.
    /// </summary>
    [JsonPropertyName("exceptionType")]
    public string? ExceptionType { get; set; }
}

/// <summary>
/// Represents performance metrics collected during test execution.
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Gets or sets the mean execution time in milliseconds.
    /// </summary>
    [JsonPropertyName("meanMs")]
    public double MeanMs { get; set; }

    /// <summary>
    /// Gets or sets the median execution time in milliseconds.
    /// </summary>
    [JsonPropertyName("medianMs")]
    public double MedianMs { get; set; }

    /// <summary>
    /// Gets or sets the standard deviation of execution times.
    /// </summary>
    [JsonPropertyName("standardDeviation")]
    public double StandardDeviation { get; set; }

    /// <summary>
    /// Gets or sets the variance of execution times.
    /// </summary>
    [JsonPropertyName("variance")]
    public double Variance { get; set; }

    /// <summary>
    /// Gets or sets the raw execution results in milliseconds.
    /// Contains all individual execution times before outlier removal.
    /// </summary>
    [JsonPropertyName("rawExecutionResults")]
    public double[] RawExecutionResults { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Gets or sets the execution results with outliers removed in milliseconds.
    /// </summary>
    [JsonPropertyName("dataWithOutliersRemoved")]
    public double[] DataWithOutliersRemoved { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Gets or sets the lower outliers detected in the execution results.
    /// </summary>
    [JsonPropertyName("lowerOutliers")]
    public double[] LowerOutliers { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Gets or sets the upper outliers detected in the execution results.
    /// </summary>
    [JsonPropertyName("upperOutliers")]
    public double[] UpperOutliers { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Gets or sets the total number of outliers detected.
    /// </summary>
    [JsonPropertyName("totalNumOutliers")]
    public int TotalNumOutliers { get; set; }

    /// <summary>
    /// Gets or sets the sample size used for the performance test.
    /// </summary>
    [JsonPropertyName("sampleSize")]
    public int SampleSize { get; set; }

    /// <summary>
    /// Gets or sets the number of warmup iterations performed before measurement.
    /// </summary>
    [JsonPropertyName("numWarmupIterations")]
    public int NumWarmupIterations { get; set; }

    /// <summary>
    /// Gets or sets the grouping identifier for batch processing.
    /// Used to group related test cases for comparison analysis.
    /// </summary>
    [JsonPropertyName("groupingId")]
    public string? GroupingId { get; set; }
}
