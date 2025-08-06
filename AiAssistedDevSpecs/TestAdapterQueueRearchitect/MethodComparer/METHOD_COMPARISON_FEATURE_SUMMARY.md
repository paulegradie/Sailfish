# 🆕 Sailfish Method Comparisons Feature

## 🎯 Overview

We're excited to introduce **Method Comparisons**, a powerful new feature that enables automatic performance comparison of multiple algorithms within a single test run. This feature seamlessly integrates with Sailfish's existing testing framework and SailDiff statistical analysis.

## ✨ Key Features

### 🏷️ **Simple Attribute-Based Syntax**
```csharp
[SailfishMethod]
[SailfishComparison("GroupName")]
public void MyMethod() { /* implementation */ }
```

### 📊 **N×N Comparison Matrix**
- **2 methods**: Each gets 1 comparison
- **3 methods**: Each gets 2 comparisons  
- **N methods**: Each gets (N-1) comparisons
- Complete comparison matrix showing how each method performs relative to all others

### 🎭 **Perspective-Based Results**
Each method shows comparison results from its own viewpoint:
- **Method A**: "A vs B", "A vs C"
- **Method B**: "B vs A", "B vs C"
- **Method C**: "C vs A", "C vs B"

### 🎨 **Intuitive Color Coding**
- 🟢 **Green**: Statistically significantly faster (Improved)
- 🔴 **Red**: Statistically significantly slower (Regressed)
- ⚪ **Gray**: No statistically significant difference (No Change)

### 📈 **Statistical Analysis**
- Powered by SailDiff's robust statistical testing
- P-value significance testing
- Outlier detection and removal
- Multiple statistical test options (T-Test, Wilcoxon, etc.)

## 🚀 Quick Start Example

```csharp
[Sailfish(SampleSize = 100)]
public class SortingComparison
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
    }

    [SailfishMethod]
    [SailfishComparison("SortingAlgorithms")]
    public void QuickSort()
    {
        var array = _data.ToArray();
        Array.Sort(array);
    }

    [SailfishMethod]
    [SailfishComparison("SortingAlgorithms")]
    public void LinqSort()
    {
        var sorted = _data.OrderBy(x => x).ToArray();
    }
}
```

## 📋 Sample Output

When you click on the BubbleSort test result:

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

When you click on the QuickSort test result:

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

## 🎯 Use Cases

### Algorithm Comparison
Compare different implementations of the same functionality:
- Sorting algorithms (Bubble, Quick, Merge, Heap)
- Search algorithms (Linear, Binary, Hash-based)
- Serialization methods (JSON, XML, Binary, MessagePack)

### Performance Optimization
Evaluate optimization attempts:
- Before/after optimization comparisons
- Different configuration settings
- Various library implementations

### Technology Evaluation
Compare different technologies or approaches:
- Database query methods (LINQ, SQL, Stored Procedures)
- HTTP client implementations (HttpClient, RestSharp, Flurl)
- Caching strategies (Memory, Redis, File-based)

## 🔧 Advanced Features

### Multiple Comparison Groups
```csharp
// Group 1: String operations
[SailfishComparison("StringOperations")]
public void StringConcat() { }

[SailfishComparison("StringOperations")]
public void StringBuilder() { }

// Group 2: Collection operations  
[SailfishComparison("Collections")]
public void ListIteration() { }

[SailfishComparison("Collections")]
public void ArrayIteration() { }
```

### Integration with Existing Features
- Works seamlessly with `[SailfishVariable]` for parameterized testing
- Compatible with all Sailfish lifecycle methods
- Supports dependency injection and complex test setups
- Integrates with output attributes (`[WriteToMarkdown]`, `[WriteToCsv]`)

## 📚 Documentation

Comprehensive documentation is available:
- **[Method Comparisons Guide](/docs/1/method-comparisons)** - Complete usage guide
- **[SailDiff Integration](/docs/2/saildiff)** - Statistical analysis details
- **[Example Implementation](source/PerformanceTests/ExamplePerformanceTests/MethodComparisonExample.cs)** - Working code example

## 🎉 Benefits

### For Developers
- **Instant Feedback**: See how your algorithms compare immediately
- **Statistical Confidence**: Know if differences are statistically significant
- **Easy Integration**: Add comparisons to existing tests with one attribute
- **Clear Results**: Intuitive color coding and perspective-based output

### For Teams
- **Objective Comparisons**: Remove guesswork from algorithm selection
- **Performance Regression Detection**: Catch performance regressions early
- **Documentation**: Automatic generation of performance comparison reports
- **CI/CD Integration**: Include performance comparisons in automated testing

### For Projects
- **Better Architecture Decisions**: Data-driven technology choices
- **Performance Culture**: Encourage performance-conscious development
- **Knowledge Sharing**: Clear performance characteristics documentation
- **Continuous Improvement**: Track performance improvements over time

## 🚀 Getting Started

1. **Update Sailfish** to the latest version
2. **Add comparison attributes** to your existing test methods
3. **Run your tests** and see the automatic comparisons
4. **Review the results** in your IDE's test output window

The feature is designed to be **zero-configuration** and **backward-compatible** with all existing Sailfish tests.

---

**Ready to compare your algorithms? Start using Method Comparisons today!** 🎯
