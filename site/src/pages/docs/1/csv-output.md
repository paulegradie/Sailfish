---
title: CSV Output Format
---

## Introduction

Sailfish can generate comprehensive CSV files containing both individual test results and method comparison data using the `[WriteToCsv]` attribute. These files use a structured, multi-section format that's Excel-friendly and perfect for data analysis.

## Basic Usage

Apply the `[WriteToCsv]` attribute to any test class:

```csharp
[WriteToCsv]
[Sailfish(SampleSize = 100)]
public class SortBenchmarks
{
    [SailfishMethod(IsBaseline = true)]
    public void QuickSort() { /* implementation */ }

    [SailfishMethod]
    public void BubbleSort() { /* implementation */ }

    [SailfishMethod]
    public void MergeSort() { /* implementation */ }
}
```

The `[Sailfish]` class above forms an implicit class-wide comparison group — `QuickSort` is the baseline; the other two are contenders. Pass `DisableComparison = true` to `[Sailfish]` to opt out and just emit per-method rows.

## CSV Structure

The generated CSV file uses a two-section format. Each section starts with a `#`-prefixed comment header.

### Section 1: Individual Test Results

```csv
# Individual Test Results
TestClass,TestMethod,MeanTime,MedianTime,StdDev,SampleSize,ComparisonGroup,Status
SortBenchmarks,BubbleSort,45.200,44.100,3.100,100,SortBenchmarks,Success
SortBenchmarks,MergeSort,3.400,3.300,0.300,100,SortBenchmarks,Success
SortBenchmarks,QuickSort,2.100,2.000,0.300,100,SortBenchmarks,Success
```

**Fields:**
- **TestClass**: Name of the test class
- **TestMethod**: Name of the test method
- **MeanTime**: Average execution time in milliseconds
- **MedianTime**: Median execution time in milliseconds
- **StdDev**: Standard deviation of execution times
- **SampleSize**: Number of iterations executed
- **ComparisonGroup**: The comparison-group label for this method's row.
  - For methods in the **implicit class-wide group** (the default when `[Sailfish]` is set without `DisableComparison`): the class name.
  - For methods with explicit `[SailfishMethod(ComparisonGroup = "...")]`: that name.
  - Empty for methods not in any comparison group (their class has `DisableComparison = true`).
  - Comparison groups are scoped per class — the same name in two different classes is reported as two independent groups, distinguished by the `TestClass` column.
- **Status**: Test execution status (`Success` / `Failed`)

{% callout title="Where are CI95_MOE / CI99_MOE?" type="note" %}
The session CSV intentionally keeps the per-test row narrow. The CI95 / CI99 margin-of-error columns are emitted in the **per-class tracking CSV** (next to the tracking JSON used by SailDiff) and surfaced in the [Reproducibility Manifest](/docs/1/reproducibility-manifest).
{% /callout %}

### Section 2: Method Comparisons

The shape of this section depends on whether the group has a baseline:

- **Baseline mode** (`IsBaseline = true` on one member): N−1 rows. Each row compares the baseline (Method1) to one contender (Method2).
- **N×N mode** (no baseline): N×(N−1)/2 rows. Every unique pair appears once.

```csv
# Method Comparisons
ComparisonGroup,Method1,Method2,Mean1,Mean2,Ratio,CI95_Lower,CI95_Upper,q_value,Label,ChangeDescription
SortBenchmarks,QuickSort,BubbleSort,2.100,45.200,21.524,18.301,24.917,1.2e-12,Slower,Regressed
SortBenchmarks,QuickSort,MergeSort,2.100,3.400,1.619,1.412,1.856,3.4e-09,Slower,Regressed
```

**Fields:**
- **ComparisonGroup**: The comparison-group label — class name for the implicit class-wide group, or the explicit name set via `SailfishMethod(ComparisonGroup = "...")`.
- **Method1 / Method2**: The two methods being compared. In baseline mode, Method1 is always the baseline.
- **Mean1 / Mean2**: Mean execution times (ms) for each method
- **Ratio**: `Mean2 / Mean1` (unitless). Values > 1 indicate Method 2 is slower than Method 1; values < 1 indicate Method 2 is faster.
- **CI95_Lower / CI95_Upper**: 95% confidence interval endpoints for the ratio (computed on the log scale)
- **q_value**: Benjamini–Hochberg adjusted p-value across the group (multiple comparisons correction)
- **Label**: One of Improved, Similar, or Slower at α = 0.05
- **ChangeDescription**: Legacy summary for backward compatibility (Improved / Regressed / No Change)

## Session-Based Consolidation

CSV files use **session-based consolidation**, meaning:

- **Single file per session**: All test classes with `[WriteToCsv]` contribute to one file
- **Cross-class comparisons**: Method comparisons work across different test classes
- **Unique naming**: Files use session IDs and timestamps to prevent conflicts
- **Complete data**: All test results from the entire session are included

**Example filename**: `TestSession_abc12345_Results_20250803_103000.csv`

## Excel Integration

The CSV format is designed for easy Excel analysis:

### 1. Import Process

1. Open Excel
2. Go to **Data** → **Get Data** → **From Text/CSV**
3. Select your Sailfish CSV file
4. Excel will automatically detect the structure

### 2. Working with Sections

- **Comment lines** (starting with `#`) provide clear section headers
- **Consistent column structure** within each section
- **No mixed data types** in columns for reliable sorting/filtering

### 3. Analysis Examples

**Performance Analysis:**
```excel
=AVERAGE(C:C)  // Average mean time across all tests
=MAX(C:C)      // Slowest test
=MIN(C:C)      // Fastest test
```

**Comparison Analysis:**
```excel
// Filter Method Comparisons section
// Sort by Ratio to find biggest differences
// Create charts showing performance relationships
```

## Advanced Features

### Multiple Comparison Groups

A class can declare explicit `ComparisonGroup` names to split its methods into more than one comparison. Each group generates its own set of rows:

```csv
# Method Comparisons
ComparisonGroup,Method1,Method2,Mean1,Mean2,Ratio,CI95_Lower,CI95_Upper,q_value,Label,ChangeDescription
StringOperations,StringBuilder,StringConcat,8.100,15.200,1.877,1.689,2.085,1.0e-12,Slower,Regressed
StringOperations,StringBuilder,StringInterpolation,8.100,12.300,1.519,1.371,1.682,2.3e-08,Slower,Regressed
StringOperations,StringConcat,StringInterpolation,15.200,12.300,0.809,0.731,0.895,3.4e-04,Improved,Improved
Collections,ArrayIteration,ListIteration,3.200,5.400,1.688,1.523,1.870,4.1e-10,Slower,Regressed
```

The rows above are an N×N example — none of the methods is marked `IsBaseline`. Add `IsBaseline = true` to one method per group and the section shrinks to N−1 rows, with Method1 always being that baseline.

### Pair counts per group

| Methods in group | No-baseline (N×N) rows | Baseline (N−1) rows |
| --- | --- | --- |
| 2 | 1 | 1 |
| 3 | 3 | 2 |
| 4 | 6 | 3 |
| N | N × (N−1) / 2 | N − 1 |

Baseline mode is set by adding `IsBaseline = true` to exactly one `[SailfishMethod]` in the group. The CSV row count for that group shrinks accordingly and the FDR adjustment runs over the smaller set, sharpening individual q-values.

### Classes that opt out of comparison

Use `[Sailfish(DisableComparison = true)]` when a class isn't really comparing alternatives — methods then appear in the individual results section with an empty `ComparisonGroup` column and no comparison rows are emitted:

```csv
# Individual Test Results
TestClass,TestMethod,MeanTime,MedianTime,StdDev,SampleSize,ComparisonGroup,Status
SmokeChecks,OperationA,10.500,9.800,1.200,100,,Success
SmokeChecks,OperationB,1.100,1.000,0.100,100,,Success
```

## Best Practices

### 1. Organize Your Data

Use meaningful test class and method names since they appear in the CSV:

```csharp
[WriteToCsv]
[Sailfish]
public class DatabaseQueryPerformance   // Class name doubles as the implicit comparison group label
{
    [SailfishMethod(IsBaseline = true)]
    public void SimpleSelect() { }      // Descriptive method name

    [SailfishMethod]
    public void ComplexJoin() { }       // Descriptive method name
}
```

### 2. Reach for explicit groups only when you need multiple in one class

The implicit class-wide group is usually all the context the output needs — methods in `DatabaseQueryPerformance` already group under that class. Only set `ComparisonGroup = "..."` when one class genuinely has multiple distinct comparisons:

```csharp
[SailfishMethod(ComparisonGroup = "DatabaseQueries")]      // Good — explicit name needed for multi-group class
[SailfishMethod(ComparisonGroup = "SerializationMethods")] // Good
[SailfishMethod(ComparisonGroup = "Group1")]               // Poor — meaningless name
```

### 3. Configure Output Directory

Set a consistent output directory for organized results:

```csharp
var runner = SailfishRunner.CreateBuilder()
    .WithRunSettings(settings => settings
        .WithLocalOutputDirectory("./performance-results"))
    .Build();
```

### 4. Combine with Markdown

Use both output formats for comprehensive reporting:

```csharp
[WriteToMarkdown]  // Human-readable reports
[WriteToCsv]       // Data analysis
[Sailfish]
public class ComprehensiveTest { }
```

## Troubleshooting

### Empty CSV Files

If CSV files are empty or missing:

1. **Check attribute placement**: Ensure `[WriteToCsv]` is on the test class, not methods
2. **Verify test execution**: CSV is only generated after successful test completion
3. **Check output directory**: Verify the configured output directory exists and is writable

### Missing Comparisons

If method comparisons are missing from the CSV:

1. **Class is `[Sailfish]`**: comparison rows require the class to be a Sailfish test class.
2. **Not opted out**: check whether the class has `DisableComparison = true` on `[Sailfish]`.
3. **Method count**: each comparison group needs ≥ 2 methods (SF1302 warns at build time).
4. **Explicit group names match**: when using explicit `ComparisonGroup`, values are case-sensitive.
5. **Single baseline per group**: at most one method per group can set `IsBaseline = true` (SF1301 enforces this).

### Excel Import Issues

If Excel doesn't import correctly:

1. **Check file encoding**: Ensure CSV is saved as UTF-8
2. **Verify delimiters**: Use comma delimiters consistently
3. **Handle comments**: Excel may need manual handling of `#` comment lines

## Integration Examples

### CI/CD Pipeline

```yaml
- name: Run Performance Tests
  run: dotnet test --logger "console;verbosity=detailed"

- name: Upload CSV Results
  uses: actions/upload-artifact@v3
  with:
    name: performance-results
    path: "**/TestSession_*.csv"
```

### Automated Analysis

```csharp
// Read and analyze CSV results programmatically
var csvData = File.ReadAllText("TestSession_abc12345_Results_20250803_103000.csv");
var results = ParseSailfishCsv(csvData);

// Generate reports, alerts, or dashboards
if (results.HasRegressions)
{
    SendAlert($"Performance regression detected: {results.WorstRegression}");
}
```
