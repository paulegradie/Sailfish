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
/// Comprehensive unit tests for TestCompletionQueueFactory.
/// Tests factory creation, configuration validation, and queue instantiation
/// for the queue infrastructure components.
/// </summary>
public class TestCompletionQueueFactoryTests
{
    private readonly ILogger _logger;
    private readonly TestCompletionQueueFactory _factory;

    public TestCompletionQueueFactoryTests()
    {
        _logger = Substitute.For<ILogger>();
        _factory = new TestCompletionQueueFactory(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionQueueFactory(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldCreateInstance()
    {
        // Act & Assert
        _factory.ShouldNotBeNull();
    }

    #endregion

    #region CreateQueueAsync with Configuration Tests

    [Fact]
    public async Task CreateQueueAsync_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _factory.CreateQueueAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateQueueAsync_WithValidConfiguration_ShouldReturnQueue()
    {
        // Arrange
        var config = new QueueConfiguration
        {
            MaxQueueCapacity = 500,
            EnableBatchProcessing = true,
            MaxBatchSize = 25
        };

        // Act
        var queue = await _factory.CreateQueueAsync(config, CancellationToken.None);

        // Assert
        queue.ShouldNotBeNull();
        queue.ShouldBeOfType<InMemoryTestCompletionQueue>();
        queue.Configuration.ShouldNotBeNull();
        queue.Configuration.MaxQueueCapacity.ShouldBe(500);
        queue.Configuration.EnableBatchProcessing.ShouldBeTrue();
        queue.Configuration.MaxBatchSize.ShouldBe(25);
    }

    [Fact]
    public async Task CreateQueueAsync_WithValidConfiguration_ShouldLogSuccess()
    {
        // Arrange
        var config = new QueueConfiguration();

        // Act
        await _factory.CreateQueueAsync(config, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Debug, "Starting queue creation with custom configuration");
        _logger.Received().Log(LogLevel.Information, 
            Arg.Is<string>(s => s.Contains("Successfully created in-memory test completion queue")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task CreateQueueAsync_WithInvalidConfiguration_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new QueueConfiguration
        {
            MaxQueueCapacity = -1, // Invalid
            PublishTimeoutMs = 0    // Invalid
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(
            () => _factory.CreateQueueAsync(config, CancellationToken.None));
        
        exception.Message.ShouldContain("Queue configuration validation failed");
        exception.ParamName.ShouldBe("configuration");
    }

    [Fact]
    public async Task CreateQueueAsync_WithInvalidConfiguration_ShouldLogError()
    {
        // Arrange
        var config = new QueueConfiguration { MaxQueueCapacity = -1 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => _factory.CreateQueueAsync(config, CancellationToken.None));

        _logger.Received().Log(LogLevel.Error, Arg.Is<string>(s => s.Contains("Queue configuration validation failed")));
    }

    [Fact]
    public async Task CreateQueueAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var config = new QueueConfiguration();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => _factory.CreateQueueAsync(config, cts.Token));
    }

    #endregion

    #region CreateQueueAsync with Default Configuration Tests

    [Fact]
    public async Task CreateQueueAsync_WithDefaultConfiguration_ShouldReturnQueue()
    {
        // Act
        var queue = await _factory.CreateQueueAsync(CancellationToken.None);

        // Assert
        queue.ShouldNotBeNull();
        queue.ShouldBeOfType<InMemoryTestCompletionQueue>();
        queue.Configuration.ShouldNotBeNull();
        queue.Configuration.IsEnabled.ShouldBeTrue();
        queue.Configuration.MaxQueueCapacity.ShouldBe(1000);
        queue.Configuration.EnableBatchProcessing.ShouldBeTrue();
        queue.Configuration.MaxBatchSize.ShouldBe(50);
        queue.Configuration.EnableFrameworkPublishing.ShouldBeTrue();
        queue.Configuration.EnableFallbackPublishing.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateQueueAsync_WithDefaultConfiguration_ShouldLogCreation()
    {
        // Act
        await _factory.CreateQueueAsync(CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Debug, "Starting queue creation with default configuration");
        _logger.Received().Log(LogLevel.Information, "Using default queue configuration for queue creation");
        _logger.Received().Log(LogLevel.Information, 
            Arg.Is<string>(s => s.Contains("Successfully created in-memory test completion queue")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task CreateQueueAsync_WithDefaultConfiguration_ShouldUseCorrectDefaults()
    {
        // Act
        var queue = await _factory.CreateQueueAsync(CancellationToken.None);

        // Assert
        var config = queue.Configuration;
        config.IsEnabled.ShouldBeTrue();
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
        config.EnableFallbackPublishing.ShouldBeTrue();
        config.LogLevel.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task CreateQueueAsync_WithDefaultConfiguration_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => _factory.CreateQueueAsync(cts.Token));
    }

    #endregion

    #region ValidateConfigurationAsync Tests

    [Fact]
    public async Task ValidateConfigurationAsync_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _factory.ValidateConfigurationAsync(null!));
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithValidConfiguration_ShouldReturnTrue()
    {
        // Arrange
        var config = new QueueConfiguration();

        // Act
        var isValid = await _factory.ValidateConfigurationAsync(config);

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithInvalidConfiguration_ShouldReturnFalse()
    {
        // Arrange
        var config = new QueueConfiguration { MaxQueueCapacity = -1 };

        // Act
        var isValid = await _factory.ValidateConfigurationAsync(config);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateConfigurationAsync_ShouldLogValidation()
    {
        // Arrange
        var config = new QueueConfiguration();

        // Act
        await _factory.ValidateConfigurationAsync(config);

        // Assert
        _logger.Received().Log(LogLevel.Debug, "Validating queue configuration");
        _logger.Received().Log(LogLevel.Debug, 
            Arg.Is<string>(s => s.Contains("Queue configuration validation completed")),
            Arg.Any<object[]>());
    }

    #endregion

    #region GetValidationErrorsAsync Tests

    [Fact]
    public async Task GetValidationErrorsAsync_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _factory.GetValidationErrorsAsync(null!));
    }

    [Fact]
    public async Task GetValidationErrorsAsync_WithValidConfiguration_ShouldReturnEmptyArray()
    {
        // Arrange
        var config = new QueueConfiguration();

        // Act
        var errors = await _factory.GetValidationErrorsAsync(config);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetValidationErrorsAsync_WithInvalidConfiguration_ShouldReturnErrors()
    {
        // Arrange
        var config = new QueueConfiguration 
        { 
            MaxQueueCapacity = -1,
            PublishTimeoutMs = 0
        };

        // Act
        var errors = await _factory.GetValidationErrorsAsync(config);

        // Assert
        errors.ShouldNotBeEmpty();
        errors.ShouldContain("MaxQueueCapacity must be greater than 0");
        errors.ShouldContain("PublishTimeoutMs must be greater than 0");
    }

    [Fact]
    public async Task GetValidationErrorsAsync_WithValidConfiguration_ShouldLogSuccess()
    {
        // Arrange
        var config = new QueueConfiguration();

        // Act
        await _factory.GetValidationErrorsAsync(config);

        // Assert
        _logger.Received().Log(LogLevel.Debug, "Queue configuration validation passed with no errors");
    }

    [Fact]
    public async Task GetValidationErrorsAsync_WithInvalidConfiguration_ShouldLogWarning()
    {
        // Arrange
        var config = new QueueConfiguration { MaxQueueCapacity = -1 };

        // Act
        await _factory.GetValidationErrorsAsync(config);

        // Assert
        _logger.Received().Log(LogLevel.Warning, 
            Arg.Is<string>(s => s.Contains("Queue configuration validation found")),
            Arg.Any<object[]>());
    }

    #endregion

    #region GetSupportedQueueTypesAsync Tests

    [Fact]
    public async Task GetSupportedQueueTypesAsync_ShouldReturnSupportedTypes()
    {
        // Act
        var supportedTypes = await _factory.GetSupportedQueueTypesAsync();

        // Assert
        supportedTypes.ShouldNotBeNull();
        supportedTypes.Length.ShouldBe(1);
        supportedTypes[0].ShouldContain("InMemory");
        supportedTypes[0].ShouldContain("High-performance in-memory queue");
        supportedTypes[0].ShouldContain("System.Threading.Channels");
    }

    [Fact]
    public async Task GetSupportedQueueTypesAsync_ShouldLogInformation()
    {
        // Act
        await _factory.GetSupportedQueueTypesAsync();

        // Assert
        _logger.Received().Log(LogLevel.Debug, "Getting supported queue types information");
        _logger.Received().Log(LogLevel.Debug, 
            Arg.Is<string>(s => s.Contains("Returning information for")),
            Arg.Any<object[]>());
    }

    #endregion
}
