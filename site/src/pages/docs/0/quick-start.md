---
title: Quick Start Guide
---

## 1. Create a Test Project

Create a class library project and install the [Sailfish Test Adapter](https://www.nuget.org/packages/Sailfish.TestAdapter);

## 2. Write a Sailfish Test

### Basic Test

```csharp
[Sailfish]
public class Example
{
    private readonly IClient client;

    // SailfishVariable accepts an explicit list of values — this produces two runs (N=1, N=10).
    // For an inclusive range with a step, use [SailfishRangeVariable(start, count, step)].
    [SailfishVariable(1, 10)]
    public int N { get; set; }

    public Example(IClient client)
    {
        this.client = client;
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken ct)
    {
        await client.Get("/api", ct);
    }
}
```

### Method Comparison Test

Mark two or more methods with the same `ComparisonGroup` and Sailfish will compare them. Pick one as the baseline via `IsBaseline = true` for an N−1 baseline-vs-contender report, or leave all members baseline-less for an N×N matrix.

```csharp
[WriteToMarkdown]  // Generate consolidated markdown output
[WriteToCsv]       // Generate consolidated CSV output
[Sailfish(SampleSize = 50)]
public class AlgorithmComparison
{
    private List<int> _data = new();

    [SailfishGlobalSetup]
    public void Setup()
    {
        _data = Enumerable.Range(1, 1000).ToList();
    }

    [SailfishMethod(ComparisonGroup = "SortingAlgorithms", IsBaseline = true)]
    public void QuickSort()
    {
        var array = _data.ToArray();
        Array.Sort(array);
    }

    [SailfishMethod(ComparisonGroup = "SortingAlgorithms")]
    public void BubbleSort()
    {
        var array = _data.ToArray();
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
}
```

See [Method Comparisons](/docs/1/method-comparisons) for the full feature, including the no-baseline N×N mode and the SF1300/1301/1302 build-time analyzers.

## 3. Register a Dependency

```csharp
public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(
        ContainerBuilder builder,
        CancellationToken cancellationToken = default)
    {
       var typeInstance = await MyClientFactory.Create(cancellationToken);
       builder.Register(_ => typeInstance).As<IClient>();
    }
}
```

## 4. Inspect your results

### Basic Test Results

```
ReadmeExample.TestMethod

Descriptive Statistics
----------------------
| Stat   |  Time (ms) |
| ---    | ---        |
| Mean   |   111.1442 |
| Median |   107.8113 |
| StdDev |     7.4208 |
| Min    |   105.9743 |
| Max    |   119.6471 |

Outliers Removed (0)
--------------------

Adjusted Distribution (ms)
--------------------------
119.6471, 105.9743, 107.8113
```

### Method Comparison Results

The individual descriptive statistics for each method appear in the IDE Test Output window as usual. The comparison itself — ratios, 95% confidence intervals, BH-FDR q-values, and Improved/Slower/Similar labels — is written to the consolidated Markdown and CSV files when `[WriteToMarkdown]` / `[WriteToCsv]` are present.

A baseline group renders like:

```
## 🔬 Comparison Group: SortingAlgorithms (AlgorithmComparison)

### 📐 Baseline-vs-Contender (baseline = `QuickSort`, q-values via BH-FDR, α=0.05)

| Method                    | Mean      | Ratio vs Baseline | 95% CI            | q-value | Label  |
|---------------------------|-----------|-------------------|-------------------|---------|--------|
| `QuickSort` _(baseline)_  | 2.100ms   | —                 | —                 | —       | —      |
| `BubbleSort`              | 45.200ms  | 21.524x           | [18.301–24.917]   | 1.2e-12 | Slower |
```

### Output Files

When using `[WriteToMarkdown]` or `[WriteToCsv]`, consolidated files are generated:

**Markdown**: `TestSession_abc12345_MethodComparisons_2025-08-03_10-30-00.md`
- Session header (Generated, Session ID, Total Test Classes, Total Test Cases)
- Per‑group comparison sections — either a baseline table (N−1) or an N×N matrix
- Per‑class detailed results table (Method, Mean, Median, Sample Size, Status)

**CSV**: `TestSession_abc12345_Results_20250803_103000.csv`
- Excel-friendly format
- Individual test results (TestClass, TestMethod, MeanTime, MedianTime, StdDev, SampleSize, ComparisonGroup, Status)
- Pairwise comparison rows with ratios, 95% CI, q-values (BH-FDR), and labels
