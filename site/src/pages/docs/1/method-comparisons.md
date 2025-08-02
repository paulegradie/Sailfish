---
title: Method Comparisons
---

## Introduction

**Method Comparisons** enable you to automatically compare the performance of multiple methods within a single test run. This powerful feature uses the `[SailfishComparison]` attribute to group related methods and automatically generates statistical comparisons between them using SailDiff.

When you mark methods with the same comparison group name, Sailfish will:
- Execute all methods in the group
- Perform N×N statistical comparisons between all methods
- Display perspective-based results for each method
- Show statistical significance with intuitive color coding

## Basic Usage

### Simple Comparison

Mark methods with the `[SailfishComparison]` attribute using the same group name:

```csharp
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

## Understanding the Results

### Perspective-Based Output

Each method shows comparison results from its own perspective. For example, if you have three methods (A, B, C), each method will show how it compares to the other two:

**Method A Output:**
```
📊 COMPARISON RESULTS:
Group: SortingAlgorithms
Comparing: BubbleSort vs QuickSort
🔴 Performance: 99.7% slower
   Statistical Significance: Regressed
   P-Value: 0.000001
   Mean Times: 1.909ms vs 0.006ms

📊 COMPARISON RESULTS:
Group: SortingAlgorithms
Comparing: BubbleSort vs LinqSort
🔴 Performance: 95.2% slower
   Statistical Significance: Regressed
   P-Value: 0.000003
   Mean Times: 1.909ms vs 0.092ms
```

**Method B Output:**
```
📊 COMPARISON RESULTS:
Group: SortingAlgorithms
Comparing: QuickSort vs BubbleSort
🟢 Performance: 99.7% faster
   Statistical Significance: Improved
   P-Value: 0.000001
   Mean Times: 0.006ms vs 1.909ms

📊 COMPARISON RESULTS:
Group: SortingAlgorithms
Comparing: QuickSort vs LinqSort
🟢 Performance: 93.5% faster
   Statistical Significance: Improved
   P-Value: 0.000012
   Mean Times: 0.006ms vs 0.092ms
```

### Color Coding

Results use intuitive color coding based on statistical significance:

- 🟢 **Green**: Statistically significantly faster (Improved)
- 🔴 **Red**: Statistically significantly slower (Regressed)  
- ⚪ **Gray/White**: No statistically significant difference (No Change)

### Statistical Significance

The framework uses SailDiff's statistical analysis to determine significance:

- **Improved**: Method is statistically significantly faster than the compared method
- **Regressed**: Method is statistically significantly slower than the compared method
- **No Change**: No statistically significant difference detected (p-value ≥ 0.05 or SailDiff determines no meaningful change)

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

## Examples

See the complete working example in the Sailfish repository:
- `source/PerformanceTests/ExamplePerformanceTests/MethodComparisonExample.cs`

This example demonstrates:
- Multiple comparison groups
- N×N comparisons with 3+ methods
- Integration with other Sailfish features
- Best practices for test setup and isolation
