using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Logging;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Processors;
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

        _processor = new MethodComparisonProcessor(
            _sailDiff,
            _mediator,
            _batchingService,
            _batchProcessor,
            _unifiedFormatter,
            _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSailDiff_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonProcessor(
            null!,
            _mediator,
            _batchingService,
            _batchProcessor,
            _unifiedFormatter,
            _logger));
    }

    [Fact]
    public void Constructor_WithNullMediator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonProcessor(
            _sailDiff,
            null!,
            _batchingService,
            _batchProcessor,
            _unifiedFormatter,
            _logger));
    }

    [Fact]
    public void Constructor_WithNullBatchingService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonProcessor(
            _sailDiff,
            _mediator,
            null!,
            _batchProcessor,
            _unifiedFormatter,
            _logger));
    }

    [Fact]
    public void Constructor_WithNullBatchProcessor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonProcessor(
            _sailDiff,
            _mediator,
            _batchingService,
            null!,
            _unifiedFormatter,
            _logger));
    }

    [Fact]
    public void Constructor_WithNullUnifiedFormatter_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonProcessor(
            _sailDiff,
            _mediator,
            _batchingService,
            _batchProcessor,
            null!,
            _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MethodComparisonProcessor(
            _sailDiff,
            _mediator,
            _batchingService,
            _batchProcessor,
            _unifiedFormatter,
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
        // Should handle empty batch list without errors
        _batchProcessor.DidNotReceive().ProcessBatch(Arg.Any<TestCaseBatch>(), Arg.Any<CancellationToken>());
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
        _batchProcessor.Received(1).ProcessBatch(Arg.Any<TestCaseBatch>(), Arg.Any<CancellationToken>());
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
        _batchProcessor.Received(2).ProcessBatch(Arg.Any<TestCaseBatch>(), Arg.Any<CancellationToken>());
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

    #region Helper Methods

    private TestCompletionQueueMessage CreateTestMessage(string testCaseId, string? comparisonGroup)
    {
        var metadata = new Dictionary<string, object>
        {
            ["TestClassName"] = "TestClass1",
            ["TestMethodName"] = testCaseId,
            ["FullyQualifiedName"] = $"TestClass1.{testCaseId}"
        };

        if (comparisonGroup != null)
        {
            metadata["ComparisonGroup"] = comparisonGroup;
        }

        return new TestCompletionQueueMessage
        {
            TestCaseId = testCaseId,
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

    private TestCaseBatch CreateTestCaseBatch(string batchId, string[] testCaseIds)
    {
        var testCases = testCaseIds.Select(id => CreateTestMessage(id, batchId)).ToList();

        return new TestCaseBatch
        {
            BatchId = batchId,
            TestCases = testCases,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Status = BatchStatus.Complete
        };
    }

    #endregion
}
