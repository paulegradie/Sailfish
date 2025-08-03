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
        Array.Sort(array);
    }
}
```

## 3. Register a Dependency

```csharp
public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(
        ContainerBuilder builder,
        CancellationToken ct)
    {
       var typeInstance = await MyClientFactory.Create(ct);
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

When using `[SailfishComparison]`, you'll see detailed comparison results in the test output:

```
📊 PERFORMANCE COMPARISON
Group: SortingAlgorithms
==================================================

🔴 IMPACT: BubbleSort() vs QuickSort() - 95.3% slower (REGRESSED)
   P-Value: 0.000001 | Mean: 45.2ms → 2.1ms

📋 DETAILED STATISTICS:

| Metric | Primary Method | Compared Method | Change | P-Value  |
| ------ | -------------- | --------------- | ------ | -------- |
| Mean   | 45.2ms         | 2.1ms           | +95.3% | 0.000001 |
| Median | 44.1ms         | 2.0ms           | +95.0% | -        |

Statistical Test: T-Test
Alpha Level: 0.05
Sample Size: 100
Outliers Removed: 3

==================================================
```

### Output Files

When using `[WriteToMarkdown]` or `[WriteToCsv]`, consolidated files are generated:

**Markdown**: `TestSession_abc12345_Results_20250803_103000.md`
- Session summary and metadata
- Individual test results
- N×N comparison matrices
- Statistical analysis

**CSV**: `TestSession_abc12345_Results_20250803_103000.csv`
- Excel-friendly format
- Session metadata
- Individual test results
- Method comparison data
- Performance ratios and change descriptions
