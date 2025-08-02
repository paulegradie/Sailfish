# Sailfish Method Comparison Feature - Completion Guide

## Overview
This document provides step-by-step instructions to complete the method comparison feature that enables "before and after" performance comparisons between methods in the same test class.

## Current Status ✅
- ✅ `SailfishComparisonAttribute` created
- ✅ Test discovery enhanced to extract comparison metadata
- ✅ Test properties added for comparison data
- ✅ Queue system enhanced to include comparison metadata
- ✅ Basic processor framework created
- ✅ Example test class created
- ✅ Solution compiles successfully

## Remaining Work 🔧

### Phase 1: Service Registration and Configuration

#### Task 1.1: Register Comparison Processor in DI Container
**File**: `source/Sailfish.TestAdapter/Registrations/TestAdapterRegistrations.cs`
**Action**: Add the comparison processor to the service registrations

```csharp
// Add to the RegisterServices method
builder.RegisterType<MethodComparisonProcessor>()
    .As<ITestCompletionQueueProcessor>()
    .InstancePerLifetimeScope();

builder.RegisterType<MethodComparisonBatchProcessor>()
    .AsSelf()
    .InstancePerLifetimeScope();
```

#### Task 1.2: Configure Queue System for Comparison Batching
**File**: `source/Sailfish.TestAdapter/Queue/Configuration/QueueConfiguration.cs`
**Action**: Add configuration options for comparison processing

```csharp
/// <summary>
/// Gets or sets whether method comparison processing is enabled.
/// When enabled, methods marked with SailfishComparisonAttribute will be
/// grouped and compared when full test classes are executed.
/// </summary>
public bool EnableMethodComparison { get; set; } = true;

/// <summary>
/// Gets or sets the strategy for detecting full class execution vs individual method execution.
/// </summary>
public ComparisonDetectionStrategy ComparisonDetectionStrategy { get; set; } = ComparisonDetectionStrategy.ByTestCaseCount;
```

#### Task 1.3: Add Comparison Detection Strategy Enum
**File**: `source/Sailfish.TestAdapter/Queue/Configuration/ComparisonDetectionStrategy.cs` (new file)

```csharp
namespace Sailfish.TestAdapter.Queue.Configuration;

/// <summary>
/// Defines strategies for detecting when to perform method comparisons.
/// </summary>
public enum ComparisonDetectionStrategy
{
    /// <summary>
    /// Detect full class execution by counting test cases in the batch.
    /// If all methods from a class are present, perform comparisons.
    /// </summary>
    ByTestCaseCount,
    
    /// <summary>
    /// Always perform comparisons when comparison groups are complete,
    /// regardless of execution context.
    /// </summary>
    Always,
    
    /// <summary>
    /// Never perform comparisons (disabled).
    /// </summary>
    Never
}
```

### Phase 2: Implement Core Comparison Logic

#### Task 2.1: Implement Full Class Detection Logic
**File**: `source/Sailfish.TestAdapter/Queue/Processors/MethodComparisonProcessor.cs`
**Action**: Add method to detect if full class is being executed

```csharp
/// <summary>
/// Determines if a full test class is being executed based on the test cases in the batch.
/// </summary>
/// <param name="batch">The batch of test completion messages.</param>
/// <param name="testClassName">The name of the test class to check.</param>
/// <returns>True if the full class is being executed, false otherwise.</returns>
private bool IsFullClassExecution(TestCaseBatch batch, string testClassName)
{
    // Get all test cases for this class
    var classTestCases = batch.TestCases
        .Where(tc => ExtractTestClassName(tc) == testClassName)
        .ToList();

    // Get all methods from the class using reflection
    var testClass = GetTestClassType(testClassName);
    if (testClass == null) return false;

    var sailfishMethods = testClass.GetMethods()
        .Where(m => m.GetCustomAttribute<SailfishMethodAttribute>() != null)
        .ToList();

    // If we have test cases for all SailfishMethods, it's a full class execution
    var executedMethods = classTestCases
        .Select(tc => ExtractMethodName(tc))
        .Distinct()
        .ToList();

    return sailfishMethods.Count == executedMethods.Count;
}
```

#### Task 2.2: Implement SailDiff Comparison Logic
**File**: `source/Sailfish.TestAdapter/Queue/Processors/MethodComparisonProcessor.cs`
**Action**: Complete the `ProcessComparisonGroup` method

```csharp
private async Task ProcessComparisonGroup(string groupName, List<TestCompletionQueueMessage> testCases, CancellationToken cancellationToken)
{
    var beforeMethods = testCases.Where(tc => ExtractComparisonRole(tc) == "Before").ToList();
    var afterMethods = testCases.Where(tc => ExtractComparisonRole(tc) == "After").ToList();

    if (beforeMethods.Count == 0 || afterMethods.Count == 0)
    {
        _logger.Log(LogLevel.Warning,
            "Incomplete comparison group '{0}': found {1} Before methods and {2} After methods",
            groupName, beforeMethods.Count, afterMethods.Count);
        return;
    }

    // For each Before/After pair, perform SailDiff comparison
    foreach (var beforeMethod in beforeMethods)
    {
        foreach (var afterMethod in afterMethods)
        {
            await PerformMethodComparison(beforeMethod, afterMethod, groupName, cancellationToken);
        }
    }
}

private async Task PerformMethodComparison(
    TestCompletionQueueMessage beforeMethod, 
    TestCompletionQueueMessage afterMethod, 
    string groupName, 
    CancellationToken cancellationToken)
{
    try
    {
        // Extract performance data
        var beforeData = CreateTestDataFromMessage(beforeMethod);
        var afterData = CreateTestDataFromMessage(afterMethod);

        // Perform SailDiff comparison
        var comparisonResult = _sailDiff.ComputeTestCaseDiff(
            new[] { beforeMethod.TestCaseId },
            new[] { afterMethod.TestCaseId },
            afterMethod.TestCaseId,
            CreateClassExecutionSummary(beforeMethod, afterMethod),
            beforeData);

        // Format comparison results
        var comparisonOutput = FormatComparisonResults(comparisonResult, groupName);

        // Enhance test output messages
        await EnhanceTestOutputWithComparison(beforeMethod, afterMethod, comparisonOutput, cancellationToken);

        _logger.Log(LogLevel.Information,
            "Completed comparison for group '{0}': {1} vs {2}",
            groupName, beforeMethod.TestCaseId, afterMethod.TestCaseId);
    }
    catch (Exception ex)
    {
        _logger.Log(LogLevel.Error, ex,
            "Failed to perform comparison for group '{0}': {1}",
            groupName, ex.Message);
    }
}
```

### Phase 3: Data Extraction and Formatting

#### Task 3.1: Implement Data Extraction Methods
**File**: `source/Sailfish.TestAdapter/Queue/Processors/MethodComparisonProcessor.cs`
**Action**: Add helper methods for data extraction

```csharp
private PerformanceRunResult CreateTestDataFromMessage(TestCompletionQueueMessage message)
{
    return new PerformanceRunResult(
        message.TestCaseId,
        message.PerformanceMetrics.RawExecutionResults,
        message.PerformanceMetrics.DataWithOutliersRemoved,
        message.PerformanceMetrics.LowerOutliers,
        message.PerformanceMetrics.UpperOutliers,
        message.PerformanceMetrics.MedianMs,
        // Add other required properties based on PerformanceRunResult constructor
    );
}

private IClassExecutionSummary CreateClassExecutionSummary(
    TestCompletionQueueMessage beforeMethod, 
    TestCompletionQueueMessage afterMethod)
{
    // Extract class execution summary from metadata
    if (beforeMethod.Metadata.TryGetValue("ClassExecutionSummaries", out var summariesObj) &&
        summariesObj is IEnumerable<IClassExecutionSummary> summaries)
    {
        return summaries.FirstOrDefault() ?? throw new InvalidOperationException("No class execution summary found");
    }
    
    throw new InvalidOperationException("Class execution summary not found in message metadata");
}

private string FormatComparisonResults(TestCaseSailDiffResult comparisonResult, string groupName)
{
    var sb = new StringBuilder();
    sb.AppendLine($"\n=== Method Comparison Results for '{groupName}' ===");
    
    // Format the SailDiff results using existing formatters
    // This will depend on the structure of TestCaseSailDiffResult
    
    sb.AppendLine("=== End Comparison Results ===\n");
    return sb.ToString();
}
```

### Phase 4: Output Enhancement and Framework Integration

#### Task 4.1: Implement Output Enhancement
**File**: `source/Sailfish.TestAdapter/Queue/Processors/MethodComparisonProcessor.cs`
**Action**: Add method to enhance test output with comparison results

```csharp
private async Task EnhanceTestOutputWithComparison(
    TestCompletionQueueMessage beforeMethod,
    TestCompletionQueueMessage afterMethod,
    string comparisonOutput,
    CancellationToken cancellationToken)
{
    // Enhance the formatted message in both test cases
    if (beforeMethod.Metadata.TryGetValue("FormattedMessage", out var beforeMessageObj) &&
        beforeMessageObj is string beforeMessage)
    {
        var enhancedBeforeMessage = beforeMessage + comparisonOutput;
        beforeMethod.Metadata["FormattedMessage"] = enhancedBeforeMessage;
    }

    if (afterMethod.Metadata.TryGetValue("FormattedMessage", out var afterMessageObj) &&
        afterMessageObj is string afterMessage)
    {
        var enhancedAfterMessage = afterMessage + comparisonOutput;
        afterMethod.Metadata["FormattedMessage"] = enhancedAfterMessage;
    }

    // Republish enhanced FrameworkTestCaseEndNotification messages
    await PublishEnhancedFrameworkNotifications(beforeMethod, afterMethod, cancellationToken);
}

private async Task PublishEnhancedFrameworkNotifications(
    TestCompletionQueueMessage beforeMethod,
    TestCompletionQueueMessage afterMethod,
    CancellationToken cancellationToken)
{
    // Create and publish enhanced framework notifications
    // This ensures the comparison results appear in the test output window
    
    var beforeNotification = CreateFrameworkNotification(beforeMethod);
    var afterNotification = CreateFrameworkNotification(afterMethod);
    
    await _mediator.Publish(beforeNotification, cancellationToken);
    await _mediator.Publish(afterNotification, cancellationToken);
}
```

### Phase 5: Integration and Testing

#### Task 5.1: Update Queue Manager to Use Comparison Processor
**File**: `source/Sailfish.TestAdapter/Queue/Implementation/TestCompletionQueueManager.cs`
**Action**: Ensure comparison processor is registered with the queue consumer

```csharp
// In the StartAsync method, after creating the consumer:
if (configuration.EnableMethodComparison)
{
    var comparisonProcessor = container.Resolve<MethodComparisonProcessor>();
    consumer.RegisterProcessor(comparisonProcessor);
}
```

#### Task 5.2: Configure Batching Strategy
**File**: `source/Sailfish.TestAdapter/Queue/Implementation/TestCaseBatchingService.cs`
**Action**: Ensure comparison batching is used when comparison attributes are detected

```csharp
// In the DetermineBatchId method, add logic to detect comparison attributes
// and switch to ByComparisonAttribute strategy when appropriate
```

#### Task 5.3: Create Integration Test
**File**: `source/Tests.TestAdapter/MethodComparisonIntegrationTest.cs` (new file)
**Action**: Create test to verify the complete feature works end-to-end

## Testing Strategy

### Manual Testing Steps
1. **Individual Method Execution**:
   - Run single method from `MethodComparisonExample`
   - Verify only method results are shown (no comparison)

2. **Full Class Execution**:
   - Run entire `MethodComparisonExample` class
   - Verify method results + SailDiff comparisons are shown

3. **Mixed Scenarios**:
   - Test classes with both comparison and non-comparison methods
   - Test incomplete comparison groups (only Before or only After)

### Automated Testing
- Unit tests for comparison detection logic
- Unit tests for data extraction methods
- Integration tests for end-to-end functionality

## Success Criteria
- ✅ Individual method execution shows only method results
- ✅ Full class execution shows method results + comparisons
- ✅ Comparison results are properly formatted and displayed
- ✅ Backward compatibility maintained
- ✅ No performance impact when feature is disabled
- ✅ Proper error handling for edge cases

## Implementation Order
1. Phase 1: Service Registration (Tasks 1.1-1.3)
2. Phase 2: Core Logic (Tasks 2.1-2.2)
3. Phase 3: Data Handling (Task 3.1)
4. Phase 4: Output Enhancement (Task 4.1)
5. Phase 5: Integration (Tasks 5.1-5.3)

Each phase should be completed and tested before moving to the next phase.
