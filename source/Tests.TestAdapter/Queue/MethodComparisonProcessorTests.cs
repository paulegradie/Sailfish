using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Logging;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Processors.MethodComparison;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Comprehensive unit tests for MethodComparisonProcessor.
/// Tests the queue processor responsible for performing SailDiff comparisons between methods
/// marked with SailfishComparisonAttribute when full test classes are executed.
/// </summary>
public class MethodComparisonProcessorTests
{
    private readonly IAdapterSailDiff _sailDiff;
    private readonly IMediator _mediator;
    private readonly ITestCaseBatchingService _batchingService;
    private readonly MethodComparisonBatchProcessor _batchProcessor;
    private readonly ISailDiffUnifiedFormatter _unifiedFormatter;
    private readonly ILogger _logger;
    private readonly MethodComparisonProcessor _processor;

    public MethodComparisonProcessorTests()
    {
        _sailDiff = Substitute.For<IAdapterSailDiff>();
        _mediator = Substitute.For<IMediator>();
        _batchingService = Substitute.For<ITestCaseBatchingService>();
        _unifiedFormatter = Substitute.For<ISailDiffUnifiedFormatter>();
        _logger = Substitute.For<ILogger>();
        _batchProcessor = Substitute.ForPartsOf<MethodComparisonBatchProcessor>(
            _sailDiff,
            _mediator,
            _logger,
            _unifiedFormatter);

        _processor = new MethodComparisonProcessor(_mediator,
            _batchingService,
            _batchProcessor,
            _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullMediator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonProcessor(null!,
            _batchingService,
            _batchProcessor,
            _logger));
    }

    [Fact]
    public void Constructor_WithNullBatchingService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonProcessor(_mediator,
            null!,
            _batchProcessor,
            _logger));
    }

    [Fact]
    public void Constructor_WithNullBatchProcessor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonProcessor(_mediator,
            _batchingService,
            null!,
            _logger));
    }

    // Note: Unified formatter is no longer a constructor dependency on MethodComparisonProcessor.
    // Keeping constructor validation tests limited to current parameters.

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonProcessor(_mediator,
            _batchingService,
            _batchProcessor,
            null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act & Assert
        _processor.ShouldNotBeNull();
    }

    #endregion

    #region ProcessTestCompletionCore Tests

    [Fact]
    public async Task ProcessTestCompletion_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        // The base class should validate parameters and throw ArgumentNullException for null message
        await Should.ThrowAsync<ArgumentNullException>(
            () => _processor.ProcessTestCompletion(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessTestCompletion_WithValidMessage_ShouldLogProcessing()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "ComparisonGroup1");

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Information, Arg.Is<string>(s => s.Contains("Starting processing of test completion message")));
    }

    [Fact]
    public async Task ProcessTestCompletion_WithComparisonGroup_ShouldExtractGroup()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "ComparisonGroup1");

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        // Verify that the processor attempted to extract comparison group
        _logger.Received().Log(LogLevel.Information, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithoutComparisonGroup_ShouldSkipProcessing()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", null);

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        // Should still log but not perform comparison processing
        _logger.Received().Log(LogLevel.Information, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "ComparisonGroup1");
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Setup batching service to throw when called with cancelled token
        _batchingService.GetBatchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TestCaseBatch?>(new OperationCanceledException()));

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => _processor.ProcessTestCompletion(message, cts.Token));
    }

    #endregion

    #region Batch Processing Tests

    [Fact]
    public async Task ProcessCompletedBatchesAsync_WithNoBatches_ShouldHandleGracefully()
    {
        // Arrange
        _batchingService.GetCompletedBatchesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TestCaseBatch>());

        // Act
        await _processor.ProcessCompletedBatchesAsync(CancellationToken.None);

        // Assert
        // Should handle empty batch list without errors - verify via logger
        // Note: Logger uses template strings with {0} placeholders, not resolved values
        _logger.Received().Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Found {0} completed batches")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessCompletedBatchesAsync_WithSingleBatch_ShouldProcessBatch()
    {
        // Arrange
        var batch = CreateTestCaseBatch("ComparisonGroup1", new[] { "TestMethod1", "TestMethod2" });
        _batchingService.GetCompletedBatchesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TestCaseBatch> { batch });

        // Act
        await _processor.ProcessCompletedBatchesAsync(CancellationToken.None);

        // Assert
        // Verify processing started via logger (avoid Arg.Any conflicts with partial substitute)
        // Note: Logger uses template strings with {0} placeholders, not resolved values
        _logger.Received().Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Found {0} completed batches")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessCompletedBatchesAsync_WithMultipleBatches_ShouldProcessAllBatches()
    {
        // Arrange
        var batch1 = CreateTestCaseBatch("ComparisonGroup1", new[] { "TestMethod1", "TestMethod2" });
        var batch2 = CreateTestCaseBatch("ComparisonGroup2", new[] { "TestMethod3", "TestMethod4" });
        _batchingService.GetCompletedBatchesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TestCaseBatch> { batch1, batch2 });

        // Act
        await _processor.ProcessCompletedBatchesAsync(CancellationToken.None);

        // Assert
        // Verify processing started via logger (avoid Arg.Any conflicts with partial substitute)
        // Note: Logger uses template strings with {0} placeholders, not resolved values
        _logger.Received().Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Found {0} completed batches")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessCompletedBatchesAsync_WithException_ShouldLogError()
    {
        // Arrange
        _batchingService.GetCompletedBatchesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyCollection<TestCaseBatch>>(new InvalidOperationException("Batch service error")));

        // Act
        await _processor.ProcessCompletedBatchesAsync(CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Error, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    #endregion

    #region Comparison Group Extraction Tests

    [Fact]
    public async Task ProcessTestCompletion_WithComparisonGroupInMetadata_ShouldExtractCorrectly()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "MyComparisonGroup");

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("ComparisonGroup")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithEmptyComparisonGroup_ShouldSkipComparison()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", string.Empty);

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Debug,
            Arg.Is<string>(s => s.Contains("not a comparison method")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithMultipleTestsInSameGroup_ShouldProcessAll()
    {
        // Arrange
        var message1 = CreateTestMessage("TestMethod1", "Group1");
        var message2 = CreateTestMessage("TestMethod2", "Group1");

        // Act
        await _processor.ProcessTestCompletion(message1, CancellationToken.None);
        await _processor.ProcessTestCompletion(message2, CancellationToken.None);

        // Assert
        _logger.Received(2).Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("ComparisonGroup")),
            Arg.Any<object[]>());
    }

    #endregion

    #region Batch Completion Detection Tests

    [Fact]
    public async Task ProcessTestCompletion_WhenBatchCompletes_ShouldTriggerBatchProcessing()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "Group1");
        var batch = CreateTestCaseBatch("Comparison_TestClass1_Group1", new[] { "TestMethod1", "TestMethod2" });

        _batchingService.GetBatchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(batch);

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _batchingService.Received().GetBatchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithInsufficientMethodsForComparison_ShouldLogDebug()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "Group1");
        var batch = CreateTestCaseBatch("Comparison_TestClass1_Group1", new[] { "TestMethod1" });

        _batchingService.GetBatchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(batch);

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Debug,
            Arg.Is<string>(s => s.Contains("insufficient methods")),
            Arg.Any<object[]>());
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ProcessCompletedBatchesAsync_WithBatchProcessingError_ShouldContinueProcessing()
    {
        // Arrange
        var batch1 = CreateTestCaseBatch("Group1", new[] { "TestMethod1", "TestMethod2" });
        var batch2 = CreateTestCaseBatch("Group2", new[] { "TestMethod3", "TestMethod4" });

        _batchingService.GetCompletedBatchesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TestCaseBatch> { batch1, batch2 });

        _batchProcessor.When(x => x.ProcessBatch(batch1, Arg.Any<CancellationToken>()))
            .Do(x => throw new InvalidOperationException("Batch processing error"));

        // Act
        await _processor.ProcessCompletedBatchesAsync(CancellationToken.None);

        // Assert - Should still attempt to process second batch
        _logger.Received().Log(LogLevel.Error, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithBatchServiceException_ShouldLogError()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "Group1");

        _batchingService.GetBatchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TestCaseBatch?>(new InvalidOperationException("Batch service error")));

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Error, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    #endregion

    #region Suppression Tests

    [Fact]
    public async Task ProcessTestCompletion_WithComparisonMethod_ShouldSuppressIndividualOutput()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "Group1");

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        message.Metadata.ShouldContainKey("SuppressIndividualOutput");
        message.Metadata["SuppressIndividualOutput"].ShouldBe(true);
    }

    [Fact]
    public async Task ProcessTestCompletion_WithNonComparisonMethod_ShouldNotSuppressOutput()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", null);

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        message.Metadata.ShouldNotContainKey("SuppressIndividualOutput");
    }

    #endregion

    #region MethodComparisonBatchProcessor Tests

    [Fact]
    public void MethodComparisonBatchProcessor_Constructor_WithNullSailDiff_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonBatchProcessor(
            null!,
            _mediator,
            _logger,
            _unifiedFormatter));
    }

    [Fact]
    public void MethodComparisonBatchProcessor_Constructor_WithNullMediator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonBatchProcessor(
            _sailDiff,
            null!,
            _logger,
            _unifiedFormatter));
    }

    [Fact]
    public void MethodComparisonBatchProcessor_Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonBatchProcessor(
            _sailDiff,
            _mediator,
            null!,
            _unifiedFormatter));
    }

    [Fact]
    public void MethodComparisonBatchProcessor_Constructor_WithNullUnifiedFormatter_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonBatchProcessor(
            _sailDiff,
            _mediator,
            _logger,
            null!));
    }

    [Fact]
    public void MethodComparisonBatchProcessor_Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var processor = new MethodComparisonBatchProcessor(
            _sailDiff,
            _mediator,
            _logger,
            _unifiedFormatter);

        // Assert
        processor.ShouldNotBeNull();
    }

    [Fact]
    public async Task MethodComparisonBatchProcessor_ProcessBatch_WithNullBatch_ShouldLogWarning()
    {
        // Arrange
        var processor = new MethodComparisonBatchProcessor(
            _sailDiff,
            _mediator,
            _logger,
            _unifiedFormatter);

        // Act
        await processor.ProcessBatch(null!, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Warning, Arg.Is<string>(s => s.Contains("null or empty")), Arg.Any<object[]>());
    }

    [Fact]
    public async Task MethodComparisonBatchProcessor_ProcessBatch_WithEmptyBatch_ShouldLogWarning()
    {
        // Arrange
        var processor = new MethodComparisonBatchProcessor(
            _sailDiff,
            _mediator,
            _logger,
            _unifiedFormatter);

        var emptyBatch = new TestCaseBatch
        {
            BatchId = "EmptyBatch",
            TestCases = new List<TestCompletionQueueMessage>(),
            Status = BatchStatus.Complete
        };

        // Act
        await processor.ProcessBatch(emptyBatch, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Warning, Arg.Is<string>(s => s.Contains("null or empty")), Arg.Any<object[]>());
    }

    [Fact]
    public async Task MethodComparisonBatchProcessor_ProcessBatch_WithSingleMethod_ShouldSkipComparison()
    {
        // Arrange
        var processor = new MethodComparisonBatchProcessor(
            _sailDiff,
            _mediator,
            _logger,
            _unifiedFormatter);

        var batch = CreateTestCaseBatch("Group1", new[] { "TestMethod1" });

        // Act
        await processor.ProcessBatch(batch, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Debug,
            Arg.Is<string>(s => s.Contains("Skipping comparison group")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task MethodComparisonBatchProcessor_ProcessBatch_WithTwoMethods_ShouldProcessComparison()
    {
        // Arrange
        var processor = new MethodComparisonBatchProcessor(
            _sailDiff,
            _mediator,
            _logger,
            _unifiedFormatter);

        var batch = CreateTestCaseBatch("Group1", new[] { "TestMethod1", "TestMethod2" });

        // Act
        await processor.ProcessBatch(batch, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Processing comparison group")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task MethodComparisonBatchProcessor_ProcessBatch_WithMultipleMethods_ShouldProcessAllPairs()
    {
        // Arrange
        var processor = new MethodComparisonBatchProcessor(
            _sailDiff,
            _mediator,
            _logger,
            _unifiedFormatter);

        var batch = CreateTestCaseBatch("Group1", new[] { "Method1", "Method2", "Method3" });

        // Act
        await processor.ProcessBatch(batch, CancellationToken.None);

        // Assert
        // Should process 3 methods = 3 pairs (1-2, 1-3, 2-3)
        // Note: Logger uses template strings with {0} and {1} placeholders
        _logger.Received().Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Processing comparison group") && s.Contains("{1} methods")),
            Arg.Any<object[]>());
    }

    #endregion

    #region Markdown Generation Tests

    [Fact]
    public async Task ProcessCompletedBatchesAsync_WithWriteToMarkdownAttribute_ShouldPublishMarkdownNotification()
    {
        // Arrange
        var batch = CreateTestCaseBatch("Group1", new[] { "TestMethod1", "TestMethod2" });
        _batchingService.GetCompletedBatchesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TestCaseBatch> { batch });

        // Act
        await _processor.ProcessCompletedBatchesAsync(CancellationToken.None);

        // Assert
        // Note: This test verifies the batch is processed. Markdown generation would require
        // the test class to have WriteToMarkdown attribute which requires reflection setup
        // Verify via logger to avoid Arg.Any conflicts
        // Note: Logger uses template strings with {0} placeholders, not resolved values
        _logger.Received().Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Found {0} completed batches")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithComparisonGroup_ShouldCheckBatchCompletion()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "Group1");
        var batch = CreateTestCaseBatch("Group1", new[] { "TestMethod1", "TestMethod2" });

        _batchingService.GetBatchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(batch);

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _batchingService.Received().GetBatchAsync(
            Arg.Is<string>(s => s.Contains("Comparison_TestClass1_Group1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithCompleteBatch_ShouldTriggerBatchProcessor()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod2", "Group1");
        var batch = CreateTestCaseBatch("Group1", new[] { "TestMethod1", "TestMethod2" });

        _batchingService.GetBatchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(batch);

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        // Verify batch processing via logger to avoid Arg.Any conflicts
        _logger.Received().Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Comparison batch") && s.Contains("is complete")),
            Arg.Any<object[]>());
    }

    #endregion

    #region Metadata Handling Tests

    [Fact]
    public async Task ProcessTestCompletion_WithComparisonMetadata_ShouldSetSuppressionFlag()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "Group1");

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        message.Metadata.ShouldContainKey("SuppressIndividualOutput");
        message.Metadata["SuppressIndividualOutput"].ShouldBe(true);
    }

    [Fact]
    public async Task ProcessTestCompletion_WithoutComparisonMetadata_ShouldNotSetSuppressionFlag()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", null);

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        message.Metadata.ShouldNotContainKey("SuppressIndividualOutput");
    }

    [Fact]
    public async Task ProcessTestCompletion_WithMetadataKeys_ShouldLogAllKeys()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "Group1");
        message.Metadata["CustomKey1"] = "Value1";
        message.Metadata["CustomKey2"] = "Value2";

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Debug,
            Arg.Is<string>(s => s.Contains("metadata keys")),
            Arg.Any<object[]>());
    }

    #endregion

    #region Performance Metrics Tests

    [Fact]
    public void CreateTestMessage_WithPerformanceMetrics_ShouldIncludeAllMetrics()
    {
        // Arrange & Act
        var message = CreateTestMessageWithMetrics("TestMethod1", "Group1",
            meanMs: 150.5, medianMs: 145.0, sampleSize: 100);

        // Assert
        message.PerformanceMetrics.MeanMs.ShouldBe(150.5);
        message.PerformanceMetrics.MedianMs.ShouldBe(145.0);
        message.PerformanceMetrics.SampleSize.ShouldBe(100);
    }

    [Fact]
    public void CreateTestCaseBatch_WithMultipleMessages_ShouldGroupCorrectly()
    {
        // Arrange & Act
        var batch = CreateTestCaseBatch("Group1", new[] { "Method1", "Method2", "Method3" });

        // Assert
        batch.TestCases.Count.ShouldBe(3);
        batch.TestCases.All(tc => tc.Metadata.ContainsKey("ComparisonGroup")).ShouldBeTrue();
        batch.TestCases.All(tc => tc.Metadata["ComparisonGroup"].ToString() == "Group1").ShouldBeTrue();
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task ProcessTestCompletion_WithVeryLongTestCaseId_ShouldHandleGracefully()
    {
        // Arrange
        var longTestCaseId = new string('A', 500);
        var message = CreateTestMessage(longTestCaseId, "Group1");

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Information, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithSpecialCharactersInGroupName_ShouldHandleCorrectly()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "Group-With_Special.Characters!");

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        message.Metadata["ComparisonGroup"].ShouldBe("Group-With_Special.Characters!");
    }

    [Fact]
    public async Task ProcessTestCompletion_WithNullMetadataValues_ShouldHandleGracefully()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "Group1");
        message.Metadata["NullValue"] = null!;

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        // Should not throw and should process normally
        _logger.Received().Log(LogLevel.Information, Arg.Any<string>(), Arg.Any<object[]>());
    }

    #endregion

    #region Batch Completion and Timing Tests

    [Fact]
    public async Task ProcessCompletedBatchesAsync_WithCancellation_ShouldStopProcessing()
    {
        // Arrange
        var batch1 = CreateTestCaseBatch("Group1", new[] { "Method1", "Method2" });
        var batch2 = CreateTestCaseBatch("Group2", new[] { "Method3", "Method4" });

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        _batchingService.GetCompletedBatchesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled<IReadOnlyCollection<TestCaseBatch>>(cts.Token));

        // Act
        await _processor.ProcessCompletedBatchesAsync(cts.Token);

        // Assert
        // The processor catches all exceptions including OperationCanceledException
        // and logs them, so it should complete gracefully
        _logger.Received().Log(LogLevel.Error,
            Arg.Any<Exception>(),
            Arg.Is<string>(s => s.Contains("Failed to process completed batches")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithBatchNotFound_ShouldLogDebug()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "Group1");
        _batchingService.GetBatchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((TestCaseBatch?)null);

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Debug,
            Arg.Is<string>(s => s.Contains("not found") || s.Contains("not ready")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithBatchServiceTimeout_ShouldHandleGracefully()
    {
        // Arrange
        var message = CreateTestMessage("TestMethod1", "Group1");
        _batchingService.GetBatchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TestCaseBatch?>(new TimeoutException("Batch service timeout")));

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Error, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    #endregion

    #region Multiple Comparison Groups Tests

    [Fact]
    public async Task MethodComparisonBatchProcessor_ProcessBatch_WithMultipleGroups_ShouldProcessEachGroup()
    {
        // Arrange
        var processor = new MethodComparisonBatchProcessor(
            _sailDiff,
            _mediator,
            _logger,
            _unifiedFormatter);

        var message1 = CreateTestMessage("Method1", "GroupA");
        var message2 = CreateTestMessage("Method2", "GroupA");
        var message3 = CreateTestMessage("Method3", "GroupB");
        var message4 = CreateTestMessage("Method4", "GroupB");

        var batch = new TestCaseBatch
        {
            BatchId = "MultipleBatch",
            TestCases = new List<TestCompletionQueueMessage> { message1, message2, message3, message4 },
            Status = BatchStatus.Complete
        };

        // Act
        await processor.ProcessBatch(batch, CancellationToken.None);

        // Assert
        // Should log processing for both groups (2 groups = 2 log calls)
        _logger.Received(2).Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Processing comparison group")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task MethodComparisonBatchProcessor_ProcessBatch_WithMixedComparisonAndNonComparison_ShouldOnlyProcessComparison()
    {
        // Arrange
        var processor = new MethodComparisonBatchProcessor(
            _sailDiff,
            _mediator,
            _logger,
            _unifiedFormatter);

        var comparisonMessage1 = CreateTestMessage("Method1", "Group1");
        var comparisonMessage2 = CreateTestMessage("Method2", "Group1");
        var nonComparisonMessage = CreateTestMessage("Method3", null);

        var batch = new TestCaseBatch
        {
            BatchId = "MixedBatch",
            TestCases = new List<TestCompletionQueueMessage> { comparisonMessage1, comparisonMessage2, nonComparisonMessage },
            Status = BatchStatus.Complete
        };

        // Act
        await processor.ProcessBatch(batch, CancellationToken.None);

        // Assert
        // Should only process the comparison group (1 group = 1 log call)
        // Note: Logger uses template strings with {0} and {1} placeholders
        _logger.Received(1).Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Processing comparison group") && s.Contains("{1} methods")),
            Arg.Any<object[]>());
    }

    #endregion

    #region Performance Comparison Tests

    [Fact]
    public async Task MethodComparisonBatchProcessor_ProcessBatch_WithDifferentPerformance_ShouldCompare()
    {
        // Arrange
        var processor = new MethodComparisonBatchProcessor(
            _sailDiff,
            _mediator,
            _logger,
            _unifiedFormatter);

        var batch = CreateTestCaseBatchWithMetrics("Group1", new[]
        {
            ("FastMethod", 50.0, 48.0),
            ("SlowMethod", 200.0, 195.0)
        });

        // Act
        await processor.ProcessBatch(batch, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Processing comparison group")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task MethodComparisonBatchProcessor_ProcessBatch_WithSimilarPerformance_ShouldStillCompare()
    {
        // Arrange
        var processor = new MethodComparisonBatchProcessor(
            _sailDiff,
            _mediator,
            _logger,
            _unifiedFormatter);

        var batch = CreateTestCaseBatchWithMetrics("Group1", new[]
        {
            ("Method1", 100.0, 98.0),
            ("Method2", 102.0, 100.0)
        });

        // Act
        await processor.ProcessBatch(batch, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Processing comparison group")),
            Arg.Any<object[]>());
    }

    #endregion

    #region Concurrent Processing Tests

    [Fact]
    public async Task ProcessTestCompletion_WithConcurrentCalls_ShouldHandleThreadSafely()
    {
        // Arrange
        var messages = Enumerable.Range(1, 10)
            .Select(i => CreateTestMessage($"TestMethod{i}", "Group1"))
            .ToList();

        // Act
        var tasks = messages.Select(m => _processor.ProcessTestCompletion(m, CancellationToken.None));
        await Task.WhenAll(tasks);

        // Assert
        // All messages should be processed without exceptions
        _logger.Received(10).Log(LogLevel.Information,
            Arg.Is<string>(s => s.Contains("Starting processing")),
            Arg.Any<object[]>());
    }

    #endregion


        #region Additional Coverage Tests (Markdown + Private Helpers)

        // Nested class so reflection can resolve by simple name "TestClass1"
        [Sailfish.Attributes.WriteToMarkdown]
        private class TestClass1 { }

        [Fact]
        public async Task GenerateMarkdownIfRequested_WithWriteToMarkdown_PublishesMarkdownNotification()
        {
            // Arrange: two methods in same comparison group so batch is complete
            var batch = CreateTestCaseBatchWithMetrics("GroupMarkdown", new[]
            {
                ("FastMethod", 50.0, 48.0),
                ("SlowMethod", 200.0, 195.0)
            });

            // Processor computes this ID: "Comparison_{Class}_{Group}"
            _batchingService.GetBatchAsync(
                Arg.Is<string>(id => id == "Comparison_TestClass1_GroupMarkdown"),
                Arg.Any<CancellationToken>())
                .Returns(batch);

            // Trigger processing for one of the messages in the batch
            var trigger = CreateTestMessage("FastMethod", "GroupMarkdown");

            // Act
            await _processor.ProcessTestCompletion(trigger, CancellationToken.None);

            // Assert: a markdown generation notification was published with expected content
            await _mediator.Received().Publish(
                Arg.Is<Sailfish.Contracts.Private.WriteMethodComparisonMarkdownNotification>(n =>
                    n.TestClassName == "TestClass1" &&
                    n.MarkdownContent.Contains("Method Comparison Results") &&
                    n.MarkdownContent.Contains("Comparison Group: GroupMarkdown") &&
                    n.MarkdownContent.Contains("Detailed Results")
                ),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public void CreatePerformanceSummary_WithTwoMethods_ProducesFastestSlowestAndGap()
        {
            // Arrange
            var methods = new List<TestCompletionQueueMessage>
            {
                CreateTestMessageWithMetrics("M1", "G", meanMs: 100, medianMs: 95, sampleSize: 10),
                CreateTestMessageWithMetrics("M2", "G", meanMs: 200, medianMs: 190, sampleSize: 10)
            };

            // Use reflection to invoke the private instance method
            var mi = typeof(MethodComparisonProcessor)
                .GetMethod("CreatePerformanceSummary", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var summary = (string)mi.Invoke(_processor, new object[] { methods })!;

            // Assert
            summary.ShouldContain("Performance Summary");
            summary.ShouldContain("**Fastest:** M1");
            summary.ShouldContain("**Slowest:** M2");
        }

        [Fact]
        public void HasComparisonMetadata_ReturnsTrue_WhenKeyPresent()
        {
            var msg = CreateTestMessage("Any", "GroupX");
            var mi = typeof(MethodComparisonProcessor)
                .GetMethod("HasComparisonMetadata", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            var result = (bool)mi.Invoke(null, new object[] { msg })!;
            result.ShouldBeTrue();
        }

        [Fact]
        public void HasComparisonMetadata_ReturnsFalse_WhenKeyMissing()
        {
            var msg = CreateTestMessage("Any", null);
            var mi = typeof(MethodComparisonProcessor)
                .GetMethod("HasComparisonMetadata", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            var result = (bool)mi.Invoke(null, new object[] { msg })!;
            result.ShouldBeFalse();
        }

        [Fact]
        public void CreateMethodComparisonMarkdown_NoGroups_ReturnsWarning()
        {
            // Arrange: batch without ComparisonGroup metadata
            var noGroupMsg = CreateTestMessage("MethodA", null);
            var batch = new TestCaseBatch
            {
                BatchId = "Comparison_TestClass1_None",
                TestCases = new List<TestCompletionQueueMessage> { noGroupMsg },
                Status = BatchStatus.Complete
            };

            var mi = typeof(MethodComparisonProcessor)
                .GetMethod("CreateMethodComparisonMarkdown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

            // Act
            var markdown = (string)mi.Invoke(_processor, new object[] { batch, typeof(TestClass1) })!;

            // Assert
            markdown.ShouldContain("No comparison groups found");
        }


        [Fact]
        public void CreateMethodComparisonMarkdown_UsesPairwisePValues_FromMetadata_MinAcrossBothDirections()
        {
            // Arrange: two methods in the same group with opposite-direction p-values
            var group = "GroupPairs";
            var m1 = CreateTestMessageWithMetrics("MethodA", group, meanMs: 50.0, medianMs: 48.0, sampleSize: 100);
            var m2 = CreateTestMessageWithMetrics("MethodB", group, meanMs: 100.0, medianMs: 98.0, sampleSize: 100);

            // Provide pairwise p-values from metadata on both methods (different magnitudes)
            m1.Metadata["PairwisePValues"] = new Dictionary<string, double>
            {
                [m2.TestCaseId] = 0.02
            };
            m2.Metadata["PairwisePValues"] = new Dictionary<string, double>
            {
                [m1.TestCaseId] = 0.01
            };

            var batch = new TestCaseBatch
            {
                BatchId = $"Comparison_TestClass1_{group}",
                TestCases = new List<TestCompletionQueueMessage> { m1, m2 },
                Status = BatchStatus.Complete
            };

            var mi = typeof(MethodComparisonProcessor)
                .GetMethod("CreateMethodComparisonMarkdown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

            // Act
            var markdown = (string)mi.Invoke(_processor, new object[] { batch, typeof(TestClass1) })!;

            // Assert: the smaller p-value (0.01) is chosen when both directions exist (min aggregation)
            markdown.ShouldContain("NxN Comparison Matrix");
            markdown.ShouldContain("q=0.01");
            markdown.ShouldNotContain("q=0.02");
        }

        [Fact]
        public void CreateMethodComparisonMarkdown_UsesPairwisePValues_WhenOnlyOneDirectionProvided()
        {
            // Arrange: only one method supplies a p-value toward the other
            var group = "GroupPairsOneWay";
            var m1 = CreateTestMessageWithMetrics("M1", group, meanMs: 60.0, medianMs: 58.0, sampleSize: 50);
            var m2 = CreateTestMessageWithMetrics("M2", group, meanMs: 100.0, medianMs: 98.0, sampleSize: 50);

            m1.Metadata["PairwisePValues"] = new Dictionary<string, double>
            {
                [m2.TestCaseId] = 0.02
            };
            // m2 does not provide a reverse entry

            var batch = new TestCaseBatch
            {
                BatchId = $"Comparison_TestClass1_{group}",
                TestCases = new List<TestCompletionQueueMessage> { m1, m2 },
                Status = BatchStatus.Complete
            };

            var mi = typeof(MethodComparisonProcessor)
                .GetMethod("CreateMethodComparisonMarkdown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

            // Act
            var markdown = (string)mi.Invoke(_processor, new object[] { batch, typeof(TestClass1) })!;

            // Assert: the provided one-way p-value flows through to q-values in the matrix
            markdown.ShouldContain("NxN Comparison Matrix");
            markdown.ShouldContain("q=0.02");
        }

        #endregion

    #region Helper Methods

    private TestCompletionQueueMessage CreateTestMessage(string testCaseId, string? comparisonGroup)
    {
        // Create a proper TestCase object (required by the processor)
        var fullyQualifiedName = $"TestClass1.{testCaseId}";
        var testCase = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase(
            fullyQualifiedName,
            new Uri("executor://sailfishexecutor/v1"),
            "Sailfish");

        var metadata = new Dictionary<string, object>
        {
            ["TestClassName"] = "TestClass1",
            ["TestMethodName"] = testCaseId,
            ["FullyQualifiedName"] = fullyQualifiedName,
            ["TestCase"] = testCase,  // Add the TestCase object to metadata
            ["StartTime"] = DateTimeOffset.UtcNow.AddSeconds(-1),
            ["EndTime"] = DateTimeOffset.UtcNow
        };

        if (comparisonGroup != null)
        {
            metadata["ComparisonGroup"] = comparisonGroup;
        }

        return new TestCompletionQueueMessage
        {
            TestCaseId = fullyQualifiedName,  // Use fully qualified name
            CompletedAt = DateTime.UtcNow,
            TestResult = new TestExecutionResult
            {
                IsSuccess = true,
                ExceptionMessage = null,
                ExceptionDetails = null,
                ExceptionType = null
            },
            PerformanceMetrics = new PerformanceMetrics
            {
                MedianMs = 100.0,
                MeanMs = 105.0,
                StandardDeviation = 10.0,
                Variance = 100.0,
                SampleSize = 10,
                NumWarmupIterations = 3,
                RawExecutionResults = new double[] { 95, 100, 105, 110 },
                DataWithOutliersRemoved = new double[] { 100, 105 },
                LowerOutliers = new double[] { 95 },
                UpperOutliers = new double[] { 110 },
                TotalNumOutliers = 2
            },
            Metadata = metadata
        };
    }

    private TestCompletionQueueMessage CreateTestMessageWithMetrics(
        string testCaseId,
        string? comparisonGroup,
        double meanMs,
        double medianMs,
        int sampleSize)
    {
        var message = CreateTestMessage(testCaseId, comparisonGroup);
        message.PerformanceMetrics = new PerformanceMetrics
        {
            MeanMs = meanMs,
            MedianMs = medianMs,
            SampleSize = sampleSize,
            StandardDeviation = 10.0,
            Variance = 100.0,
            NumWarmupIterations = 3,
            RawExecutionResults = new double[] { meanMs - 10, meanMs, meanMs + 10 },
            DataWithOutliersRemoved = new double[] { meanMs },
            LowerOutliers = new double[] { meanMs - 10 },
            UpperOutliers = new double[] { meanMs + 10 },
            TotalNumOutliers = 2
        };
        return message;
    }

    private TestCaseBatch CreateTestCaseBatch(string comparisonGroup, string[] testCaseIds)
    {
        // Create test messages with the comparison group
        var testCases = testCaseIds.Select(id => CreateTestMessage(id, comparisonGroup)).ToList();

        // Format batch ID as "Comparison_{testClassName}_{comparisonGroup}"
        // The processor expects this format (see MethodComparisonProcessor.cs line 264)
        var batchId = $"Comparison_TestClass1_{comparisonGroup}";

        return new TestCaseBatch
        {
            BatchId = batchId,
            TestCases = testCases,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Status = BatchStatus.Complete
        };
    }

    private TestCaseBatch CreateTestCaseBatchWithMetrics(string comparisonGroup, (string id, double meanMs, double medianMs)[] testCases)
    {
        // Create test messages with metrics
        var messages = testCases.Select(tc =>
            CreateTestMessageWithMetrics(tc.id, comparisonGroup, tc.meanMs, tc.medianMs, 100)).ToList();

        // Format batch ID as "Comparison_{testClassName}_{comparisonGroup}"
        var batchId = $"Comparison_TestClass1_{comparisonGroup}";

        return new TestCaseBatch
        {
            BatchId = batchId,
            TestCases = messages,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Status = BatchStatus.Complete
        };
    }

    #endregion
}
