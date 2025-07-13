# Sailfish Method Comparison Feature

## Overview

The Sailfish Method Comparison feature allows you to mark multiple methods within a single test class for direct performance comparison. Methods with the same comparison group will be executed in the same test run and automatically compared using statistical analysis.

## Key Features

- **Direct Method Comparison**: Compare multiple methods within a single test run
- **Statistical Analysis**: Automatic statistical significance testing using existing SailDiff infrastructure
- **Performance Rankings**: Methods are ranked by performance with relative performance metrics
- **Baseline Support**: Optionally specify a baseline method for relative comparisons
- **Backward Compatibility**: Fully compatible with existing Sailfish tests

## Usage

### Basic Usage

Mark methods for comparison using the `[SailfishMethodComparison]` attribute:

```csharp
[Sailfish]
public class AlgorithmComparison
{
    [SailfishVariable(100, 1000, 10000)]
    public int DataSize { get; set; }

    [SailfishMethodComparison("SortingAlgorithms")]
    [SailfishMethod]
    public void BubbleSort()
    {
        // Implementation
    }

    [SailfishMethodComparison("SortingAlgorithms")]
    [SailfishMethod]
    public void QuickSort()
    {
        // Implementation
    }

    [SailfishMethodComparison("SortingAlgorithms")]
    [SailfishMethod]
    public void MergeSort()
    {
        // Implementation
    }
}
```

### Advanced Usage with Baseline

Specify a baseline method for relative performance comparisons:

```csharp
[SailfishMethodComparison("SearchAlgorithms", BaselineMethod = "LinearSearch")]
[SailfishMethod]
public void LinearSearch()
{
    // Implementation
}

[SailfishMethodComparison("SearchAlgorithms")]
[SailfishMethod]
public void BinarySearch()
{
    // Implementation
}
```

### Custom Significance Level

Adjust the statistical significance level for comparisons:

```csharp
[SailfishMethodComparison("Algorithms", SignificanceLevel = 0.01)]
[SailfishMethod]
public void Method1()
{
    // Implementation
}
```

## Attribute Properties

### SailfishMethodComparisonAttribute

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `ComparisonGroup` | `string` | Yes | - | Name of the comparison group |
| `BaselineMethod` | `string` | No | `null` | Name of the baseline method for relative comparisons |
| `SignificanceLevel` | `double` | No | `0.05` | Statistical significance level (0.001-0.1) |
| `IncludeInComparison` | `bool` | No | `true` | Whether to include this method in comparison output |

## Output

The feature generates comprehensive comparison results including:

### Performance Rankings
```
Performance Rankings:
  1. QuickSort - 12.34ms (1.00x) 🚀 Fastest
  2. MergeSort - 15.67ms (1.27x) ⚡ Fast
  3. BubbleSort - 89.12ms (7.22x) 🐢 Slowest
```

### Statistical Analysis
```
Statistically Significant Differences:
  • BubbleSort vs baseline: p-value = 0.0001
  • MergeSort vs baseline: p-value = 0.0234
```

## Implementation Details

### Architecture

The method comparison feature integrates seamlessly with Sailfish's existing architecture:

1. **Discovery Phase**: `DiscoveryAnalysisMethods` detects `[SailfishMethodComparison]` attributes
2. **Execution Phase**: `MethodComparisonCoordinator` groups and compares methods after execution
3. **Analysis Phase**: Uses existing `StatisticalTestComputer` for statistical analysis
4. **Presentation Phase**: `MethodComparisonPresenter` formats and displays results

### Key Components

- `SailfishMethodComparisonAttribute`: Marks methods for comparison
- `TestCaseComparisonGroup`: Groups related test cases
- `MethodComparisonResult`: Contains comparison results and rankings
- `MethodComparisonCoordinator`: Orchestrates comparison execution
- `MethodComparisonPresenter`: Handles result presentation

## Best Practices

1. **Group Related Methods**: Only compare methods that perform similar operations
2. **Use Consistent Test Data**: Ensure all compared methods use the same input data
3. **Choose Appropriate Baselines**: Select a well-known or standard algorithm as baseline
4. **Consider Statistical Significance**: Use appropriate significance levels for your domain
5. **Minimize External Factors**: Ensure test environment consistency

## Limitations

- Requires at least 2 methods in a comparison group
- Methods must be in the same test class
- All methods in a group must use the same `[SailfishVariable]` parameters
- Statistical analysis requires sufficient sample sizes for meaningful results

## Examples

See `source/PerformanceTests/ExamplePerformanceTests/MethodComparisonExample.cs` for a complete working example demonstrating:

- Sorting algorithm comparisons with baseline
- Search algorithm comparisons
- Multiple comparison groups in one class
- Various performance indicators and rankings
