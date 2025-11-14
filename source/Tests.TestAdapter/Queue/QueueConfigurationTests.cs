using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Configuration;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Comprehensive unit tests for QueueConfiguration.
/// Tests configuration validation, default values, and property settings
/// for the queue system configuration model.
/// </summary>
public class QueueConfigurationTests
{
    #region Default Values Tests

    [Fact]
    public void Constructor_ShouldSetCorrectDefaultValues()
    {
        // Act
        var config = new QueueConfiguration();

        // Assert
        config.IsEnabled.ShouldBeFalse();
        config.MaxQueueCapacity.ShouldBe(1000);
        config.PublishTimeoutMs.ShouldBe(5000);
        config.ProcessingTimeoutMs.ShouldBe(30000);
        config.BatchCompletionTimeoutMs.ShouldBe(60000);
        config.MaxRetryAttempts.ShouldBe(3);
        config.BaseRetryDelayMs.ShouldBe(1000);
        config.EnableBatchProcessing.ShouldBeTrue();
        config.MaxBatchSize.ShouldBe(50);
        config.EnableFrameworkPublishing.ShouldBeTrue();
        config.EnableLoggingProcessor.ShouldBeFalse();
        config.EnableComparisonAnalysis.ShouldBeFalse();
        config.EnableMethodComparison.ShouldBeTrue();
        config.ComparisonDetectionStrategy.ShouldBe(ComparisonDetectionStrategy.ByTestCaseCount);
        config.ComparisonTimeoutMs.ShouldBe(30000);
        config.EnableFallbackPublishing.ShouldBeTrue();
        config.LogLevel.ShouldBe(LogLevel.Information);
    }

    #endregion

    #region Property Setting Tests

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new QueueConfiguration();

        // Act
        config.IsEnabled = true;
        config.MaxQueueCapacity = 2000;
        config.PublishTimeoutMs = 10000;
        config.ProcessingTimeoutMs = 60000;
        config.BatchCompletionTimeoutMs = 120000;
        config.MaxRetryAttempts = 5;
        config.BaseRetryDelayMs = 2000;
        config.EnableBatchProcessing = false;
        config.MaxBatchSize = 100;
        config.EnableFrameworkPublishing = false;
        config.EnableLoggingProcessor = true;
        config.EnableComparisonAnalysis = true;
        config.EnableMethodComparison = false;
        config.ComparisonDetectionStrategy = ComparisonDetectionStrategy.Always;
        config.ComparisonTimeoutMs = 45000;
        config.EnableFallbackPublishing = false;
        config.LogLevel = LogLevel.Debug;

        // Assert
        config.IsEnabled.ShouldBeTrue();
        config.MaxQueueCapacity.ShouldBe(2000);
        config.PublishTimeoutMs.ShouldBe(10000);
        config.ProcessingTimeoutMs.ShouldBe(60000);
        config.BatchCompletionTimeoutMs.ShouldBe(120000);
        config.MaxRetryAttempts.ShouldBe(5);
        config.BaseRetryDelayMs.ShouldBe(2000);
        config.EnableBatchProcessing.ShouldBeFalse();
        config.MaxBatchSize.ShouldBe(100);
        config.EnableFrameworkPublishing.ShouldBeFalse();
        config.EnableLoggingProcessor.ShouldBeTrue();
        config.EnableComparisonAnalysis.ShouldBeTrue();
        config.EnableMethodComparison.ShouldBeFalse();
        config.ComparisonDetectionStrategy.ShouldBe(ComparisonDetectionStrategy.Always);
        config.ComparisonTimeoutMs.ShouldBe(45000);
        config.EnableFallbackPublishing.ShouldBeFalse();
        config.LogLevel.ShouldBe(LogLevel.Debug);
    }

    #endregion

    #region Validation Tests - Valid Configurations

    [Fact]
    public void Validate_WithDefaultConfiguration_ShouldReturnNoErrors()
    {
        // Arrange
        var config = new QueueConfiguration();

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WithValidCustomConfiguration_ShouldReturnNoErrors()
    {
        // Arrange
        var config = new QueueConfiguration
        {
            MaxQueueCapacity = 500,
            PublishTimeoutMs = 3000,
            ProcessingTimeoutMs = 15000,
            BatchCompletionTimeoutMs = 30000,
            MaxRetryAttempts = 2,
            BaseRetryDelayMs = 500,
            MaxBatchSize = 25,
            LogLevel = LogLevel.Warning
        };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldBeEmpty();
    }

    #endregion

    #region Validation Tests - Invalid Configurations

    [Fact]
    public void Validate_WithZeroMaxQueueCapacity_ShouldReturnError()
    {
        // Arrange
        var config = new QueueConfiguration { MaxQueueCapacity = 0 };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldContain("MaxQueueCapacity must be greater than 0");
    }

    [Fact]
    public void Validate_WithNegativeMaxQueueCapacity_ShouldReturnError()
    {
        // Arrange
        var config = new QueueConfiguration { MaxQueueCapacity = -100 };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldContain("MaxQueueCapacity must be greater than 0");
    }

    [Fact]
    public void Validate_WithZeroPublishTimeoutMs_ShouldReturnError()
    {
        // Arrange
        var config = new QueueConfiguration { PublishTimeoutMs = 0 };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldContain("PublishTimeoutMs must be greater than 0");
    }

    [Fact]
    public void Validate_WithNegativeProcessingTimeoutMs_ShouldReturnError()
    {
        // Arrange
        var config = new QueueConfiguration { ProcessingTimeoutMs = -1000 };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldContain("ProcessingTimeoutMs must be greater than 0");
    }

    [Fact]
    public void Validate_WithZeroBatchCompletionTimeoutMs_ShouldReturnError()
    {
        // Arrange
        var config = new QueueConfiguration { BatchCompletionTimeoutMs = 0 };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldContain("BatchCompletionTimeoutMs must be greater than 0");
    }

    [Fact]
    public void Validate_WithNegativeMaxRetryAttempts_ShouldReturnError()
    {
        // Arrange
        var config = new QueueConfiguration { MaxRetryAttempts = -1 };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldContain("MaxRetryAttempts must be greater than or equal to 0");
    }

    [Fact]
    public void Validate_WithZeroBaseRetryDelayMs_ShouldReturnError()
    {
        // Arrange
        var config = new QueueConfiguration { BaseRetryDelayMs = 0 };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldContain("BaseRetryDelayMs must be greater than 0");
    }

    [Fact]
    public void Validate_WithZeroMaxBatchSize_ShouldReturnError()
    {
        // Arrange
        var config = new QueueConfiguration { MaxBatchSize = 0 };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldContain("MaxBatchSize must be greater than 0");
    }

    [Fact]
    public void Validate_WithInvalidLogLevel_ShouldReturnError()
    {
        // Arrange
        var config = new QueueConfiguration { LogLevel = (LogLevel)999 };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldContain("LogLevel must be a valid LogLevel enum value");
    }

    [Fact]
    public void Validate_WithMaxBatchSizeGreaterThanCapacity_ShouldReturnError()
    {
        // Arrange
        var config = new QueueConfiguration
        {
            MaxQueueCapacity = 100,
            MaxBatchSize = 200,
            EnableBatchProcessing = true
        };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldContain("MaxBatchSize cannot be greater than MaxQueueCapacity when batch processing is enabled");
    }

    [Fact]
    public void Validate_WithMaxBatchSizeEqualToCapacity_ShouldReturnNoErrors()
    {
        // Arrange
        var config = new QueueConfiguration
        {
            MaxQueueCapacity = 100,
            MaxBatchSize = 100,
            EnableBatchProcessing = true
        };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WithBatchProcessingDisabled_ShouldIgnoreBatchSizeValidation()
    {
        // Arrange
        var config = new QueueConfiguration
        {
            MaxQueueCapacity = 100,
            MaxBatchSize = 200,
            EnableBatchProcessing = false
        };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldNotContain(e => e.Contains("MaxBatchSize cannot be greater than MaxQueueCapacity"));
    }

    #endregion

    #region Multiple Validation Errors Tests

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var config = new QueueConfiguration
        {
            MaxQueueCapacity = -1,
            PublishTimeoutMs = 0,
            ProcessingTimeoutMs = -100,
            BatchCompletionTimeoutMs = 0,
            MaxRetryAttempts = -1,
            BaseRetryDelayMs = 0,
            MaxBatchSize = 0,
            ComparisonTimeoutMs = 0
        };

        // Act
        var errors = config.Validate();

        // Assert
        errors.Length.ShouldBe(9);
        errors.ShouldContain("MaxQueueCapacity must be greater than 0");
        errors.ShouldContain("PublishTimeoutMs must be greater than 0");
        errors.ShouldContain("ProcessingTimeoutMs must be greater than 0");
        errors.ShouldContain("BatchCompletionTimeoutMs must be greater than 0");
        errors.ShouldContain("MaxRetryAttempts must be greater than or equal to 0");
        errors.ShouldContain("BaseRetryDelayMs must be greater than 0");
        errors.ShouldContain("MaxBatchSize must be greater than 0");
        errors.ShouldContain("ComparisonTimeoutMs must be greater than 0");
        errors.ShouldContain("MaxBatchSize cannot be greater than MaxQueueCapacity when batch processing is enabled");
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void Validate_WithZeroMaxRetryAttempts_ShouldReturnNoErrors()
    {
        // Arrange
        var config = new QueueConfiguration { MaxRetryAttempts = 0 };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WithVeryLargeValues_ShouldReturnNoErrors()
    {
        // Arrange
        var config = new QueueConfiguration
        {
            MaxQueueCapacity = int.MaxValue,
            PublishTimeoutMs = int.MaxValue,
            ProcessingTimeoutMs = int.MaxValue,
            BatchCompletionTimeoutMs = int.MaxValue,
            MaxRetryAttempts = int.MaxValue,
            BaseRetryDelayMs = int.MaxValue,
            MaxBatchSize = int.MaxValue
        };

        // Act
        var errors = config.Validate();

        // Assert
        errors.ShouldBeEmpty();
    }

    #endregion
}
