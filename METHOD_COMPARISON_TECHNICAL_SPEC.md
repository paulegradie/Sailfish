# Method Comparison Feature - Technical Specification

## Architecture Overview

### Current Queue Flow
```
Test Completion → TestCaseCompletedNotificationHandler → Queue → Processors → Framework
```

### Enhanced Flow with Method Comparison
```
Test Completion → Handler (+ comparison metadata) → Queue → Batching Service → 
Comparison Processor → Enhanced Results → Framework Publishing Processor → Framework
```

## Key Components and Their Responsibilities

### 1. TestCaseCompletedNotificationHandler
**Status**: ✅ Complete
- Extracts comparison metadata from test cases
- Includes metadata in TestCompletionQueueMessage
- Routes to queue or direct publishing based on configuration

### 2. TestCaseBatchingService
**Status**: ✅ Partially Complete
- Groups test cases by comparison criteria
- Detects when comparison groups are complete
- **Needs**: Enhanced logic to detect full class execution

### 3. MethodComparisonProcessor
**Status**: 🔧 Needs Implementation
- Processes individual test completion messages
- **Current**: Basic framework exists
- **Needs**: Full implementation of comparison logic

### 4. MethodComparisonBatchProcessor
**Status**: 🔧 Needs Implementation
- Processes complete batches for comparison analysis
- **Needs**: Complete implementation

## Data Flow Specification

### Test Case Properties
```csharp
// Added to TestCase during discovery
SailfishComparisonGroupProperty: "OptimizationTest"
SailfishComparisonRoleProperty: "Before" | "After"
```

### Queue Message Metadata
```csharp
// Added to TestCompletionQueueMessage.Metadata
"ComparisonGroup": "OptimizationTest"
"ComparisonRole": "Before"
"TestCase": TestCase object
"FormattedMessage": string (for enhancement)
"ClassExecutionSummaries": IEnumerable<IClassExecutionSummary>
```

### Batch Identification
```csharp
// Batch ID format for comparison groups
"Comparison_{TestClassName}_{ComparisonGroup}"
// Example: "Comparison_MethodComparisonExample_SumCalculation"
```

## Implementation Details

### Phase 1: Service Registration

#### File Locations and Changes
1. **TestAdapterRegistrations.cs**
   - Add `MethodComparisonProcessor` registration
   - Add `MethodComparisonBatchProcessor` registration

2. **QueueConfiguration.cs**
   - Add `EnableMethodComparison` property
   - Add `ComparisonDetectionStrategy` property

3. **New File: ComparisonDetectionStrategy.cs**
   - Define enum for detection strategies

### Phase 2: Core Logic Implementation

#### Critical Methods to Implement

1. **IsFullClassExecution()**
   ```csharp
   // Logic: Compare executed methods vs all SailfishMethods in class
   // Use reflection to get all methods with SailfishMethodAttribute
   // Compare with methods present in batch
   ```

2. **ProcessComparisonGroup()**
   ```csharp
   // Group Before/After methods
   // Validate complete pairs exist
   // Call PerformMethodComparison for each pair
   ```

3. **PerformMethodComparison()**
   ```csharp
   // Extract performance data from messages
   // Call IAdapterSailDiff.ComputeTestCaseDiff
   // Format results
   // Enhance output messages
   ```

### Phase 3: Data Extraction

#### Performance Data Mapping
```csharp
// Map TestCompletionQueueMessage.PerformanceMetrics to PerformanceRunResult
PerformanceRunResult {
    DisplayName = message.TestCaseId,
    RawExecutionResults = message.PerformanceMetrics.RawExecutionResults,
    DataWithOutliersRemoved = message.PerformanceMetrics.DataWithOutliersRemoved,
    // ... other properties
}
```

#### Class Execution Summary Extraction
```csharp
// Extract from message metadata
var summaries = (IEnumerable<IClassExecutionSummary>)message.Metadata["ClassExecutionSummaries"];
```

### Phase 4: Output Enhancement

#### Message Enhancement Strategy
1. Extract original formatted message from metadata
2. Append comparison results
3. Update metadata with enhanced message
4. Republish FrameworkTestCaseEndNotification

#### Comparison Output Format
```
Original test output...

=== Method Comparison Results for 'OptimizationTest' ===
Comparing: CalculateSumWithLinq (Before) vs CalculateSumWithLoop (After)

Statistical Test: Two-Sample Wilcoxon Signed Rank Test
Significance Level: 0.001

Before (CalculateSumWithLinq):
  Median: 15.2ms
  Mean: 15.8ms
  Std Dev: 2.1ms

After (CalculateSumWithLoop):
  Median: 8.7ms
  Mean: 9.1ms
  Std Dev: 1.3ms

Result: FASTER (p-value: 0.0001)
Performance Improvement: 42.8%
=== End Comparison Results ===
```

## Error Handling Strategy

### Graceful Degradation
- If comparison fails, continue with normal test execution
- Log errors but don't fail the test run
- Fallback to direct framework publishing if queue fails

### Edge Cases
1. **Incomplete Comparison Groups**: Log warning, continue execution
2. **Missing Performance Data**: Skip comparison, log warning
3. **SailDiff Computation Failure**: Log error, continue with original results
4. **Individual Method Execution**: Skip comparison processing

## Configuration Options

### QueueConfiguration Properties
```csharp
public class QueueConfiguration
{
    // Existing properties...
    
    /// <summary>
    /// Enables method comparison processing. Default: true
    /// </summary>
    public bool EnableMethodComparison { get; set; } = true;
    
    /// <summary>
    /// Strategy for detecting when to perform comparisons. Default: ByTestCaseCount
    /// </summary>
    public ComparisonDetectionStrategy ComparisonDetectionStrategy { get; set; } = 
        ComparisonDetectionStrategy.ByTestCaseCount;
    
    /// <summary>
    /// Timeout for comparison processing in milliseconds. Default: 30000 (30 seconds)
    /// </summary>
    public int ComparisonTimeoutMs { get; set; } = 30000;
}
```

## Testing Strategy

### Unit Tests Required
1. **Comparison Detection Tests**
   - Test full class vs individual method detection
   - Test various batch compositions

2. **Data Extraction Tests**
   - Test performance data mapping
   - Test metadata extraction

3. **Comparison Logic Tests**
   - Test SailDiff integration
   - Test result formatting

### Integration Tests Required
1. **End-to-End Comparison Test**
   - Execute MethodComparisonExample class
   - Verify comparison results in output

2. **Backward Compatibility Test**
   - Ensure non-comparison tests work unchanged
   - Verify performance impact is minimal

### Manual Testing Scenarios
1. Run individual method → No comparison shown
2. Run full class → Comparisons shown
3. Run class with incomplete groups → Warnings logged, execution continues
4. Disable feature → No comparison processing

## Performance Considerations

### Memory Usage
- Batch processing requires holding multiple test results in memory
- Implement proper disposal of large objects
- Consider batch size limits for large test classes

### Processing Time
- Comparison processing adds overhead to test execution
- Implement timeouts to prevent hanging
- Consider async processing to avoid blocking

### Scalability
- Design supports multiple comparison groups per class
- Supports multiple Before/After pairs per group
- Efficient batching prevents excessive memory usage

## Backward Compatibility

### Existing Tests
- All existing tests continue to work unchanged
- No impact on tests without comparison attributes
- Queue system remains optional (fallback to direct publishing)

### Configuration
- Feature enabled by default but can be disabled
- Graceful degradation when components are missing
- No breaking changes to existing APIs

## Future Enhancements

### Potential Extensions
1. **Multiple Comparison Strategies**: Support different statistical tests
2. **Historical Comparisons**: Compare against previous test runs
3. **Threshold-Based Alerts**: Alert when performance degrades beyond threshold
4. **Custom Comparison Formatters**: Allow custom output formatting
5. **Comparison Reports**: Generate detailed comparison reports

### Architecture Extensibility
- Processor pipeline supports additional processors
- Batching strategies can be extended
- Output formatters can be customized
- Detection strategies can be added
