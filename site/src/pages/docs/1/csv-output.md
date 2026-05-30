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
public class PerformanceTest
{
    [SailfishMethod(ComparisonGroup = "Algorithms", IsBaseline = true)]
    public void QuickSort() { /* implementation */ }

    [SailfishMethod(ComparisonGroup = "Algorithms")]
    public void BubbleSort() { /* implementation */ }

    [SailfishMethod]
    public void RegularMethod() { /* implementation */ }
}
```

## CSV Structure

The generated CSV file uses a two-section format. Each section starts with a `#`-prefixed comment header.

### Section 1: Individual Test Results

```csv
# Individual Test Results
TestClass,TestMethod,MeanTime,MedianTime,StdDev,SampleSize,ComparisonGroup,Status
PerformanceTest,BubbleSort,45.200,44.100,3.100,100,Algorithms,Success
PerformanceTest,QuickSort,2.100,2.000,0.300,100,Algorithms,Success
PerformanceTest,RegularMethod,1.000,1.000,0.100,100,,Success
```

**Fields:**
- **TestClass**: Name of the test class
- **TestMethod**: Name of the test method
- **MeanTime**: Average execution time in milliseconds
- **MedianTime**: Median execution time in milliseconds
- **StdDev**: Standard deviation of execution times
- **SampleSize**: Number of iterations executed
- **ComparisonGroup**: Comparison group name (set via `SailfishMethod(ComparisonGroup = "...")`); empty for ungrouped methods. The group is scoped per test class — the same name in two different classes is reported as two independent groups, distinguished by the `TestClass` column.
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
Algorithms,QuickSort,BubbleSort,2.100,45.200,21.524,18.301,24.917,1.2e-12,Slower,Regressed
```

**Fields:**
- **ComparisonGroup**: Name of the comparison group
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

When you have multiple comparison groups, each generates its own set of comparisons:

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

### Mixed Test Types

The CSV includes both comparison and regular methods:

```csv
# Individual Test Results
TestClass,TestMethod,MeanTime,MedianTime,StdDev,SampleSize,ComparisonGroup,Status
MyTest,ComparisonMethod1,10.500,9.800,1.200,100,Group1,Success
MyTest,ComparisonMethod2,8.300,8.100,0.900,100,Group1,Success
MyTest,RegularMethod,1.000,1.000,0.100,100,,Success
MyTest,AnotherRegularMethod,1.100,1.000,0.100,100,,Success
```

## Best Practices

### 1. Organize Your Data

Use meaningful test class and method names since they appear in the CSV:

```csharp
[WriteToCsv]
public class DatabaseQueryPerformance  // Clear class name
{
    [SailfishMethod(ComparisonGroup = "QueryTypes", IsBaseline = true)]
    public void SimpleSelect() { }      // Descriptive method name

    [SailfishMethod(ComparisonGroup = "QueryTypes")]
    public void ComplexJoin() { }       // Descriptive method name
}
```

### 2. Use Descriptive Comparison Groups

Choose comparison group names that clearly indicate what's being compared:

```csharp
[SailfishMethod(ComparisonGroup = "DatabaseQueries")]      // Good
[SailfishMethod(ComparisonGroup = "SerializationMethods")] // Good
[SailfishMethod(ComparisonGroup = "Group1")]               // Poor
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

1. **Verify group names**: Ensure methods use identical `ComparisonGroup` values (case-sensitive)
2. **Check method count**: Need at least 2 methods in a group for comparisons (SF1302 warns at build time)
3. **Single baseline per group**: At most one method in the group can set `IsBaseline = true` (SF1301 enforces this)

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
