---
title: Method Comparisons
---

## Introduction

**Method Comparisons** enable you to automatically compare the performance of multiple methods within a single test run. This powerful feature uses the `[SailfishComparison]` attribute to group related methods and automatically generates statistical comparisons between them using SailDiff.

When you mark methods with the same comparison group name, Sailfish will:
- Execute all methods in the group
- Perform N×N statistical comparisons between all methods
- Display perspective-based results for each method in the test output
- Show statistical significance with intuitive color coding
- Generate consolidated comparison data in markdown and CSV outputs (when using `[WriteToMarkdown]` or `[WriteToCsv]` attributes)

## Basic Usage

### Simple Comparison

Mark methods with the `[SailfishComparison]` attribute using the same group name:

```csharp
[WriteToMarkdown]  // Optional: Generate consolidated markdown output
[WriteToCsv]       // Optional: Generate consolidated CSV output
[Sailfish(SampleSize = 100)]
public class AlgorithmComparison
{
    private List<int> _data = new();

    [SailfishGlobalSetup]
    public void Setup()
    {
        _data = Enumerable.Range(1, 1000).ToList();
    }

    [SailfishMethod]
    [SailfishComparison("SortingAlgorithms")]
    public void BubbleSort()
    {
        var array = _data.ToArray();
        // Bubble sort implementation
        for (int i = 0; i < array.Length - 1; i++)
        {
            for (int j = 0; j < array.Length - i - 1; j++)
            {
                if (array[j] > array[j + 1])
                {
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
                }
            }
        }
    }

    [SailfishMethod]
    [SailfishComparison("SortingAlgorithms")]
    public void QuickSort()
    {
        var array = _data.ToArray();
        Array.Sort(array); // Built-in optimized sort
    }

    [SailfishMethod]
    [SailfishComparison("SortingAlgorithms")]
    public void LinqSort()
    {
        var sorted = _data.OrderBy(x => x).ToArray();
    }
}
```

### Multiple Comparison Groups

You can have multiple comparison groups in the same test class:

```csharp
[WriteToMarkdown]  // Generate consolidated markdown with comparison matrices
[WriteToCsv]       // Generate consolidated CSV with comparison data
[Sailfish(SampleSize = 50)]
public class MultipleComparisons
{
    // Group 1: String operations
    [SailfishMethod]
    [SailfishComparison("StringOperations")]
    public void StringConcat() { /* implementation */ }

    [SailfishMethod]
    [SailfishComparison("StringOperations")]
    public void StringBuilder() { /* implementation */ }

    // Group 2: Collection operations
    [SailfishMethod]
    [SailfishComparison("Collections")]
    public void ListIteration() { /* implementation */ }

    [SailfishMethod]
    [SailfishComparison("Collections")]
    public void ArrayIteration() { /* implementation */ }

    // Regular method (not part of any comparison)
    [SailfishMethod]
    public void RegularMethod() { /* implementation */ }
}
```

## Output Formats

Method comparison results are available in multiple formats:

### 1. Test Output Window (IDE/Console)

Real-time perspective-based results shown during test execution. Each method displays its comparison results from its own perspective.

### 2. Consolidated Markdown Files

When using `[WriteToMarkdown]`, generates session-based markdown files containing:
- Session metadata and summary statistics
- Individual test results for all methods
- N×N comparison matrices for each comparison group
- Statistical analysis with p-values and significance testing

**Example filename**: `TestSession_abc12345_Results_20250803_103000.md`

{% callout title="See also" type="note" %}
Format details and troubleshooting:
- [Markdown Output](/docs/1/markdown-output)
- [CSV Output](/docs/1/csv-output)
{% /callout %}


### 3. Consolidated CSV Files

When using `[WriteToCsv]`, generates session-based CSV files containing:
- Session metadata (session ID, timestamp, test counts)
- Individual test results (all performance metrics)
- Method comparison data (performance ratios, change descriptions)
- Excel-friendly format with clear section separation

**Example filename**: `TestSession_abc12345_Results_20250803_103000.csv`

{% callout title="CSV format details" type="note" %}
See full format and complete examples here: [CSV Output](/docs/1/csv-output)
{% /callout %}

## Understanding the Results

### Individual Test Results

Each method first displays its individual performance statistics:

```
MethodComparisonExample.SortWithQuickSort

Descriptive Statistics
----------------------
| Stat   |  Time (ms) |
| ---    | ---        |
| Mean   |     0.0053 |
| Median |      0.005 |
| StdDev |     0.0005 |
| Min    |     0.0047 |
| Max    |     0.0099 |

Outliers Removed (2)
--------------------
2 Upper Outliers: 0.0091, 0.0099

Distribution (ms)
-----------------
0.0057, 0.0048, 0.0058, 0.0058, 0.0057, 0.0047, 0.0048, 0.0047...
```

### Performance Comparison Output

After individual statistics, methods show their comparison results in a comprehensive format:

```
📊 PERFORMANCE COMPARISON
Group: SortingAlgorithm
==================================================

🟢 IMPACT: SortWithQuickSort() vs SortWithBubbleSort() - 99.7% faster (IMPROVED)
   P-Value: 0.000000 | Mean: 1.730ms → 0.005ms

📋 DETAILED STATISTICS:

| Metric | Primary Method | Compared Method | Change | P-Value  |
| ------ | -------------- | --------------- | ------ | -------- |
| Mean   | 1.730ms        | 0.005ms         | -99.7% | 0.000000 |
| Median | 1.666ms        | 0.005ms         | -99.7% | -        |

Statistical Test: T-Test
Alpha Level: 0.05
Sample Size: 100
Outliers Removed: 7

==================================================
```

### Multiple Comparisons

When a method belongs to a comparison group with multiple methods, it shows comparisons against each other method:

```
📊 PERFORMANCE COMPARISON
Group: SortingAlgorithm
==================================================

🟢 IMPACT: SortWithQuickSort() vs SortWithBubbleSort() - 99.7% faster (IMPROVED)
   P-Value: 0.000000 | Mean: 1.730ms → 0.005ms

==================================================

📊 PERFORMANCE COMPARISON
Group: SortingAlgorithm
==================================================

🟢 IMPACT: SortWithQuickSort() vs SortWithOtherSort() - 100.0% faster (IMPROVED)
   P-Value: 0.000000 | Mean: 15.622ms → 0.005ms

==================================================
```

### Understanding the Output Format

**Header Section:**
- **Group**: Shows the comparison group name
- **IMPACT**: Primary comparison result with color coding and percentage change
- **P-Value**: Statistical significance value
- **Mean Transition**: Shows the performance change (before → after)

**Detailed Statistics Table:**
- **Metric**: Statistical measure (Mean, Median)
- **Primary Method**: The current method being analyzed
- **Compared Method**: The method being compared against
- **Change**: Percentage change (negative = improvement, positive = regression)
- **P-Value**: Statistical significance for each metric

**Test Information:**
- **Statistical Test**: Type of test used (T-Test, Wilcoxon, etc.)
- **Alpha Level**: Significance threshold (typically 0.05)
- **Sample Size**: Number of iterations per method
- **Outliers Removed**: Number of outliers detected and excluded

### Color Coding

Results use intuitive color coding based on statistical significance:

- 🟢 **Green**: Statistically significantly faster (IMPROVED)
- 🔴 **Red**: Statistically significantly slower (REGRESSED)
- ⚪ **Gray/White**: No statistically significant difference (NO CHANGE)

### Statistical Significance

The framework uses SailDiff's statistical analysis to determine significance:

- **IMPROVED**: Method is statistically significantly faster than the compared method
- **REGRESSED**: Method is statistically significantly slower than the compared method
- **NO CHANGE**: No statistically significant difference detected (p-value ≥ alpha level)

## Advanced Features

### N×N Comparisons

When you have multiple methods in a comparison group, the framework automatically performs N×N comparisons:

- **2 methods**: Each method gets 1 comparison
- **3 methods**: Each method gets 2 comparisons
- **4 methods**: Each method gets 3 comparisons
- **N methods**: Each method gets (N-1) comparisons

This gives you a complete comparison matrix showing how each method performs relative to all others.

### Integration with SailDiff

Method comparisons are powered by SailDiff, which provides:

- Statistical hypothesis testing
- Outlier detection and removal
- Multiple statistical test options (T-Test, Wilcoxon, etc.)
- Configurable significance levels

You can configure SailDiff behavior using `.sailfish.json`:

```json
{
  "SailDiffSettings": {
    "TestType": "TTest",
    "Alpha": 0.05,
    "Disabled": false
  }
}
```

## Best Practices

### 1. Use Meaningful Group Names

Choose descriptive names that clearly indicate what's being compared:

```csharp
[SailfishComparison("DatabaseQueries")]     // Good
[SailfishComparison("SerializationMethods")] // Good
[SailfishComparison("Group1")]               // Poor
```

### 2. Ensure Fair Comparisons

Make sure compared methods are testing equivalent functionality:

```csharp
// Good: All methods sort the same data
[SailfishComparison("SortingAlgorithms")]
public void BubbleSort() { /* sorts _data */ }

[SailfishComparison("SortingAlgorithms")]
public void QuickSort() { /* sorts _data */ }

// Poor: Methods do different things
[SailfishComparison("Mixed")]
public void SortData() { /* sorts data */ }

[SailfishComparison("Mixed")]
public void SearchData() { /* searches data - not comparable! */ }
```

### 3. Use Adequate Sample Sizes

Ensure your sample size is large enough for meaningful statistical analysis:


{% callout title="Tip: Adaptive Sampling" type="note" %}
Instead of guessing a fixed `SampleSize`, enable [Adaptive Sampling](/docs/1/adaptive-sampling). Sailfish stops when results are statistically stable (using coefficient of variation and confidence interval width thresholds), often reducing runtime while preserving rigor. Opt in per class via `[Sailfish]` or set a global policy with `RunSettingsBuilder`.
{% /callout %}

```csharp
[Sailfish(SampleSize = 100)] // Good for most comparisons
public class PerformanceComparison
{
    // Methods with small performance differences need larger samples
    // Methods with large performance differences can use smaller samples
}
```

### 4. Consider Test Isolation

Each method should be independent and not affect others:

```csharp
[SailfishGlobalSetup]
public void Setup()
{
    // Initialize shared data once
    _data = GenerateTestData();
}

[SailfishMethod]
[SailfishComparison("Algorithms")]
public void Method1()
{
    var localCopy = _data.ToArray(); // Work with copies
    // Process localCopy...
}
```

## Troubleshooting

### No Comparison Results Shown

If you don't see comparison results:

1. **Check group names**: Ensure methods use identical group names (case-sensitive)
2. **Verify attributes**: Both `[SailfishMethod]` and `[SailfishComparison]` are required
3. **Minimum methods**: You need at least 2 methods in a group for comparisons
4. **Run all tests**: Comparisons only work when running multiple tests together

### Unexpected Results

If results seem incorrect:

1. **Check test isolation**: Ensure methods don't interfere with each other
2. **Verify data consistency**: All methods should work with equivalent data
3. **Review sample size**: Increase sample size for more reliable statistics
4. **Check for outliers**: SailDiff automatically handles outliers, but extreme variations may affect results

## Complete Example

Here's a comprehensive example demonstrating all method comparison features:

```csharp
[WriteToMarkdown]  // Generate consolidated markdown output
[WriteToCsv]       // Generate consolidated CSV output
[Sailfish(DisableOverheadEstimation = true, SampleSize = 100)]
public class MethodComparisonExample
{
    private readonly List<int> _data = new();

    [SailfishGlobalSetup]
    public void Setup()
    {
        // Initialize test data
        _data.Clear();
        for (int i = 0; i < 1000; i++)
        {
            _data.Add(i);
        }
    }

    // Group 1: Sum calculation algorithms
    [SailfishMethod]
    [SailfishComparison("SumCalculation")]
    public void CalculateSumWithLinq()
    {
        var sum = _data.Sum();
        Thread.Sleep(1); // Simulate work
    }

    [SailfishMethod]
    [SailfishComparison("SumCalculation")]
    public void CalculateSumWithLoop()
    {
        var sum = 0;
        foreach (var item in _data)
        {
            sum += item;
        }
        Thread.Sleep(1); // Simulate work
    }

    // Group 2: Sorting algorithms
    [SailfishMethod]
    [SailfishComparison("SortingAlgorithm")]
    public void SortWithBubbleSort()
    {
        var array = _data.ToArray();
        // Bubble sort implementation
        for (int i = 0; i < array.Length - 1; i++)
        {
            for (int j = 0; j < array.Length - i - 1; j++)
            {
                if (array[j] > array[j + 1])
                {
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
                }
            }
        }
    }

    [SailfishMethod]
    [SailfishComparison("SortingAlgorithm")]
    public void SortWithQuickSort()
    {
        var array = _data.ToArray();
        Array.Sort(array);
    }

    // Regular method (not part of any comparison)
    [SailfishMethod]
    public void RegularMethod()
    {
        Thread.Sleep(1);
    }
}
```

## Repository Examples

See the complete working examples in the Sailfish repository:
- `source/PerformanceTests/ExamplePerformanceTests/MethodComparisonExample.cs`

This example demonstrates:
- Multiple comparison groups in a single class
- N×N comparisons with 2+ methods per group
- Integration with output attributes (`[WriteToMarkdown]`, `[WriteToCsv]`)
- Session-based consolidation across test runs
- Best practices for test setup and isolation
- Mixed usage (comparison methods + regular methods)
