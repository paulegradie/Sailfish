# Method Comparison Feature - Implementation Checklist

## 📋 Quick Start Guide

This checklist provides the exact steps to complete the method comparison feature. Each task includes file paths, code snippets, and estimated time.

## ✅ Completed
- SailfishComparisonAttribute created
- Test discovery enhanced
- Queue system enhanced with comparison metadata
- Basic processor framework created
- Solution compiles successfully

## 🔧 TODO - Ordered by Priority

### Phase 1: Service Registration (30 minutes total)

#### ☐ Task 1.1: Register Comparison Processor
**File**: `source/Sailfish.TestAdapter/Registrations/TestAdapterRegistrations.cs`
**Time**: 15 minutes
**Action**: Find the `RegisterServices` method and add these registrations:

```csharp
// Add after existing ITestCompletionQueueProcessor registrations
builder.RegisterType<MethodComparisonProcessor>()
    .As<ITestCompletionQueueProcessor>()
    .InstancePerLifetimeScope();

builder.RegisterType<MethodComparisonBatchProcessor>()
    .AsSelf()
    .InstancePerLifetimeScope();
```

#### ☐ Task 1.2: Create Detection Strategy Enum
**File**: `source/Sailfish.TestAdapter/Queue/Configuration/ComparisonDetectionStrategy.cs` (NEW FILE)
**Time**: 5 minutes
**Action**: Create new file with this content:

```csharp
namespace Sailfish.TestAdapter.Queue.Configuration;

/// <summary>
/// Defines strategies for detecting when to perform method comparisons.
/// </summary>
public enum ComparisonDetectionStrategy
{
    /// <summary>
    /// Detect full class execution by counting test cases in the batch.
    /// </summary>
    ByTestCaseCount,
    
    /// <summary>
    /// Always perform comparisons when comparison groups are complete.
    /// </summary>
    Always,
    
    /// <summary>
    /// Never perform comparisons (disabled).
    /// </summary>
    Never
}
```

#### ☐ Task 1.3: Add Configuration Properties
**File**: `source/Sailfish.TestAdapter/Queue/Configuration/QueueConfiguration.cs`
**Time**: 10 minutes
**Action**: Add these properties to the QueueConfiguration class:

```csharp
/// <summary>
/// Gets or sets whether method comparison processing is enabled.
/// </summary>
public bool EnableMethodComparison { get; set; } = true;

/// <summary>
/// Gets or sets the strategy for detecting full class execution.
/// </summary>
public ComparisonDetectionStrategy ComparisonDetectionStrategy { get; set; } = 
    ComparisonDetectionStrategy.ByTestCaseCount;

/// <summary>
/// Gets or sets the timeout for comparison processing in milliseconds.
/// </summary>
public int ComparisonTimeoutMs { get; set; } = 30000;
```

### Phase 2: Core Logic Implementation (3-4 hours total)

#### ☐ Task 2.1: Implement Full Class Detection
**File**: `source/Sailfish.TestAdapter/Queue/Processors/MethodComparisonProcessor.cs`
**Time**: 1 hour
**Action**: Add these methods to the MethodComparisonBatchProcessor class:

```csharp
private bool IsFullClassExecution(TestCaseBatch batch, string testClassName)
{
    // Get all test cases for this class
    var classTestCases = batch.TestCases
        .Where(tc => ExtractTestClassName(tc) == testClassName)
        .ToList();

    // Get the test class type
    var testClass = GetTestClassType(testClassName);
    if (testClass == null) return false;

    // Get all SailfishMethod methods
    var sailfishMethods = testClass.GetMethods()
        .Where(m => m.GetCustomAttribute<SailfishMethodAttribute>() != null)
        .Select(m => m.Name)
        .ToHashSet();

    // Get executed method names
    var executedMethods = classTestCases
        .Select(tc => ExtractMethodName(tc))
        .Distinct()
        .ToHashSet();

    // If all SailfishMethods are executed, it's a full class run
    return sailfishMethods.SetEquals(executedMethods);
}

private Type? GetTestClassType(string testClassName)
{
    // Implementation to get Type from class name
    // This may require accessing assembly information from test cases
    return null; // TODO: Implement based on available metadata
}

private string ExtractTestClassName(TestCompletionQueueMessage message)
{
    // Extract class name from test case ID or metadata
    var testCaseId = message.TestCaseId;
    var parts = testCaseId.Split('.');
    return parts.Length >= 2 ? parts[^2] : string.Empty; // Second to last part
}

private string ExtractMethodName(TestCompletionQueueMessage message)
{
    // Extract method name from test case ID
    var testCaseId = message.TestCaseId;
    var parts = testCaseId.Split('.');
    var methodPart = parts.LastOrDefault() ?? string.Empty;
    
    // Remove variable section if present (everything after first parenthesis)
    var parenIndex = methodPart.IndexOf('(');
    return parenIndex > 0 ? methodPart.Substring(0, parenIndex) : methodPart;
}
```

#### ☐ Task 2.2: Implement SailDiff Comparison Logic
**File**: `source/Sailfish.TestAdapter/Queue/Processors/MethodComparisonProcessor.cs`
**Time**: 2-3 hours
**Action**: Replace the TODO in ProcessComparisonGroup with:

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

    // Check if this is a full class execution
    var testClassName = ExtractTestClassName(beforeMethods.First());
    var batch = new TestCaseBatch { TestCases = testCases }; // Create temporary batch
    
    if (!IsFullClassExecution(batch, testClassName))
    {
        _logger.Log(LogLevel.Debug,
            "Skipping comparison for group '{0}' - not a full class execution", groupName);
        return;
    }

    _logger.Log(LogLevel.Information,
        "Processing comparison group '{0}' with {1} Before methods and {2} After methods",
        groupName, beforeMethods.Count, afterMethods.Count);

    // For each Before/After pair, perform comparison
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
        // Create comparison output
        var comparisonOutput = CreateComparisonOutput(beforeMethod, afterMethod, groupName);

        // Enhance test output messages
        EnhanceTestOutputWithComparison(beforeMethod, comparisonOutput);
        EnhanceTestOutputWithComparison(afterMethod, comparisonOutput);

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

private string CreateComparisonOutput(
    TestCompletionQueueMessage beforeMethod,
    TestCompletionQueueMessage afterMethod,
    string groupName)
{
    var sb = new StringBuilder();
    sb.AppendLine($"\n=== Method Comparison Results for '{groupName}' ===");
    sb.AppendLine($"Before: {ExtractMethodName(beforeMethod)} (Median: {beforeMethod.PerformanceMetrics.MedianMs:F2}ms)");
    sb.AppendLine($"After:  {ExtractMethodName(afterMethod)} (Median: {afterMethod.PerformanceMetrics.MedianMs:F2}ms)");
    
    var improvement = ((beforeMethod.PerformanceMetrics.MedianMs - afterMethod.PerformanceMetrics.MedianMs) / beforeMethod.PerformanceMetrics.MedianMs) * 100;
    
    if (improvement > 0)
    {
        sb.AppendLine($"Result: FASTER by {improvement:F1}%");
    }
    else
    {
        sb.AppendLine($"Result: SLOWER by {Math.Abs(improvement):F1}%");
    }
    
    sb.AppendLine("=== End Comparison Results ===\n");
    return sb.ToString();
}

private void EnhanceTestOutputWithComparison(TestCompletionQueueMessage message, string comparisonOutput)
{
    if (message.Metadata.TryGetValue("FormattedMessage", out var messageObj) && messageObj is string originalMessage)
    {
        message.Metadata["FormattedMessage"] = originalMessage + comparisonOutput;
    }
}
```

### Phase 3: Integration and Testing (1 hour total)

#### ☐ Task 3.1: Test the Implementation
**Time**: 30 minutes
**Action**: 
1. Build the solution: `dotnet build`
2. Run the example test class
3. Verify comparison output appears

#### ☐ Task 3.2: Create Simple Integration Test
**File**: `source/Tests.TestAdapter/MethodComparisonIntegrationTest.cs` (NEW FILE)
**Time**: 30 minutes
**Action**: Create a basic test to verify the feature works

## 🎯 Success Criteria

After completing all tasks, you should see:
- ✅ Solution builds without errors
- ✅ Individual method execution shows only method results
- ✅ Full class execution shows method results + comparison output
- ✅ Comparison output includes performance improvement percentages

## 🚨 Important Notes

1. **Start with Phase 1** - Service registration is required for everything else to work
2. **Test after each phase** - Don't implement everything at once
3. **Check logs** - Use the logger to debug issues
4. **Fallback behavior** - If comparison fails, tests should still execute normally

## 📞 Need Help?

If you encounter issues:
1. Check the build output for specific errors
2. Verify all using statements are correct
3. Ensure DI registrations are in the right place
4. Check that queue configuration is enabled

The technical specification (`METHOD_COMPARISON_TECHNICAL_SPEC.md`) contains additional implementation details if needed.
