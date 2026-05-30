---
title: Output Attributes
---

Sailfish prints to StdOut and writes a tracking file into the calling assemblies **bin** directory by default. You can export additional formatted documents using the following attributes.

## WriteToMarkdown

The `[WriteToMarkdown]` attribute generates consolidated markdown files containing both individual test results and method comparison data when applied to test classes.

```csharp
[WriteToMarkdown]
[Sailfish]
public class SortBenchmarks
{
    // [Sailfish] makes comparison automatic — every method is in the implicit
    // class-wide comparison group. One method as baseline switches to N−1 mode.

    [SailfishMethod(IsBaseline = true)]
    public void QuickSort() { /* ... */ }

    [SailfishMethod]
    public void BubbleSort() { /* ... */ }

    [SailfishMethod]
    public void MergeSort() { /* ... */ }
}
```

### Markdown Output Features

- **Session-based consolidation**: Single markdown file per test session
- **Per-group sections**: Either a baseline-vs-contender table (when one method is the baseline) or an N×N comparison matrix (when none is), followed by a five-column detailed results table per comparison group
- **Per-class scoping**: Section header is `## 🔬 Comparisons: {ClassName}` for the implicit class-wide group, or `## 🔬 Comparison Group: {Name} ({ClassName})` for explicit named groups — same-named groups in different classes are reported separately
- **Statistical analysis**: BH-FDR q-values and 95% ratio confidence intervals
- **Environment & reproducibility headers**: Optional `🏥 Environment Health Check` and `🔁 Reproducibility Summary` sections near the top when enabled

CI95/CI99 margins of error are not embedded in the consolidated session markdown — they're available in the per-class tracking CSV and in the Reproducibility Manifest.

**Example filename**: `TestSession_abc12345_MethodComparisons_2025-08-03_10-30-00.md`


{% callout title="See also" type="note" %}
Detailed format guides:
- [Markdown Output](/docs/1/markdown-output)
- [CSV Output](/docs/1/csv-output)
{% /callout %}

## WriteToCsv

The `[WriteToCsv]` attribute generates consolidated CSV files containing both individual test results and method comparison data in a structured, Excel-friendly format.

```csharp
[WriteToCsv]
[Sailfish]
public class SortBenchmarks
{
    [SailfishMethod(IsBaseline = true)]
    public void QuickSort() { /* ... */ }

    [SailfishMethod]
    public void BubbleSort() { /* ... */ }

    [SailfishMethod]
    public void MergeSort() { /* ... */ }
}
```

### CSV Output Features

- **Two-section format**: Individual test results and method comparison rows
- **Baseline-aware row count**: N−1 rows in baseline mode, N×(N−1)/2 rows in N×N mode
- **Excel-compatible**: Easy to import and analyze in spreadsheet applications
- **Session-based consolidation**: Single CSV file per test session
- **Numeric ratios with confidence intervals and q-values**: Comparison rows include the BH-FDR q-value and 95% ratio CI bounds

**Example filename**: `TestSession_abc12345_Results_20250803_103000.csv`

### CSV Structure

```csv
# Individual Test Results
TestClass,TestMethod,MeanTime,MedianTime,StdDev,SampleSize,ComparisonGroup,Status
SortBenchmarks,BubbleSort,45.200,44.100,3.100,100,SortBenchmarks,Success
SortBenchmarks,MergeSort,3.400,3.300,0.300,100,SortBenchmarks,Success
SortBenchmarks,QuickSort,2.100,2.000,0.300,100,SortBenchmarks,Success

# Method Comparisons
ComparisonGroup,Method1,Method2,Mean1,Mean2,Ratio,CI95_Lower,CI95_Upper,q_value,Label,ChangeDescription
SortBenchmarks,QuickSort,BubbleSort,2.100,45.200,21.524,18.301,24.917,1.2e-12,Slower,Regressed
SortBenchmarks,QuickSort,MergeSort,2.100,3.400,1.619,1.412,1.856,3.4e-09,Slower,Regressed
```

Implicit class-wide group rows show the class name in the `ComparisonGroup` column (here `SortBenchmarks`). Explicit `ComparisonGroup = "..."` values would appear there instead. With `IsBaseline = true` on one method, Method1 is always the baseline and one row appears per contender; without a baseline the rows are all pairwise (i&lt;j) combinations. Ratio = Mean2 / Mean1.

## Combined Usage

You can use both attributes together to generate both markdown and CSV outputs:

```csharp
[WriteToMarkdown]
[WriteToCsv]
[Sailfish]
public class ComprehensiveTest
{
    // Test methods...
}
```

## Session-Based Consolidation

Both output attributes use **session-based consolidation**, meaning:

- **Single file per session**: All test classes with the attribute contribute to one consolidated file
- **Cross-class comparisons**: Method comparisons work across different test classes in the same session
- **Unique naming**: Files use session IDs and timestamps to prevent conflicts
- **Complete data**: All test results from the entire session are included

## Best Practices

### 1. Use Descriptive Test Class Names
Since multiple classes may contribute to a single output file, use clear, descriptive class names:

```csharp
[WriteToCsv]
public class DatabaseQueryPerformance { }

[WriteToCsv]
public class SerializationBenchmarks { }
```

### 2. Reach for explicit groups only when you need multiple in one class
The implicit class-wide group is usually all the context the output needs — methods in `SortBenchmarks` are reported under that class name automatically. Only set `ComparisonGroup = "..."` when one class genuinely has multiple distinct comparisons to make:

```csharp
[SailfishMethod(ComparisonGroup = "DatabaseQueries")]      // Good
[SailfishMethod(ComparisonGroup = "SerializationMethods")] // Good
[SailfishMethod(ComparisonGroup = "Group1")]               // Poor — meaningless name
```

### 3. Consider Output Directory
Configure the output directory in your test settings to organize results:

```csharp
var runner = SailfishRunner.CreateBuilder()
    .WithRunSettings(settings => settings
        .WithLocalOutputDirectory("./performance-results"))
    .Build();
```

## Extensibility

{% callout title="Extensibility" type="note" %}
Sailfish exposes notification handlers that you can implement to customize output generation:

- `INotificationHandler<WriteMethodComparisonMarkdownNotification>` for markdown customization
- `INotificationHandler<WriteMethodComparisonCsvNotification>` for CSV customization

These handlers allow you to customize what is done with the generated content before it's written to files.
{% /callout %}

## Runtime diagnostics in outputs

Sailfish appends concise diagnostics to test output and logs:
- Overhead calibration: baseline ticks, drift %, and capped-iteration count
- Timer granularity note: when effective sleep resolution is coarse (e.g., Windows ~15.6 ms), short sleeps/awaits will read near the tick length

These appear in:
- Console/INF logs during `dotnet test`
- IDE Test Output window per test
- Session markdown includes a summary Environment Health section
