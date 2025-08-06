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
    [SailfishMethod]
    [SailfishComparison("Algorithms")]
    public void BubbleSort() { /* implementation */ }

    [SailfishMethod]
    [SailfishComparison("Algorithms")]
    public void QuickSort() { /* implementation */ }

    [SailfishMethod]
    public void RegularMethod() { /* implementation */ }
}
```

## CSV Structure

The generated CSV files use a multi-section format with clear organization:

### Section 1: Session Metadata

```csv
# Session Metadata
SessionId,Timestamp,TotalClasses,TotalTests
abc12345,2025-08-03T10:30:00Z,1,6
```

**Fields:**
- **SessionId**: Unique identifier for the test session
- **Timestamp**: When the test session completed (UTC)
- **TotalClasses**: Number of test classes with `[WriteToCsv]` in the session
- **TotalTests**: Total number of test methods executed

### Section 2: Individual Test Results

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
- **ComparisonGroup**: Comparison group name (if using `[SailfishComparison]`)
- **Status**: Test execution status (Success/Failed)

### Section 3: Method Comparisons

```csv
# Method Comparisons
ComparisonGroup,Method1,Method2,Method1Mean,Method2Mean,PerformanceRatio,ChangeDescription
Algorithms,BubbleSort,QuickSort,45.200,2.100,21.5x slower,Regressed
```

**Fields:**
- **ComparisonGroup**: Name of the comparison group
- **Method1**: First method in the comparison
- **Method2**: Second method in the comparison
- **Method1Mean**: Mean execution time of Method1
- **Method2Mean**: Mean execution time of Method2
- **PerformanceRatio**: Performance relationship (e.g., "21.5x slower", "2.3x faster")
- **ChangeDescription**: Statistical significance (Improved/Regressed/No Change)

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
// Sort by PerformanceRatio to find biggest differences
// Create charts showing performance relationships
```

## Advanced Features

### Multiple Comparison Groups

When you have multiple comparison groups, each generates its own set of comparisons:

```csv
# Method Comparisons
ComparisonGroup,Method1,Method2,Method1Mean,Method2Mean,PerformanceRatio,ChangeDescription
StringOperations,StringConcat,StringBuilder,15.200,8.100,1.9x slower,Regressed
StringOperations,StringConcat,StringInterpolation,15.200,12.300,1.2x slower,Regressed
StringOperations,StringBuilder,StringInterpolation,8.100,12.300,1.5x faster,Improved
Collections,ListIteration,ArrayIteration,5.400,3.200,1.7x slower,Regressed
```

### N×N Comparison Matrices

For groups with multiple methods, all pairwise comparisons are included:

- **2 methods**: 1 comparison
- **3 methods**: 3 comparisons (A vs B, A vs C, B vs C)
- **4 methods**: 6 comparisons
- **N methods**: N×(N-1)/2 comparisons

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
    [SailfishMethod]
    [SailfishComparison("QueryTypes")]
    public void SimpleSelect() { }      // Descriptive method name
    
    [SailfishMethod]
    [SailfishComparison("QueryTypes")]
    public void ComplexJoin() { }       // Descriptive method name
}
```

### 2. Use Descriptive Comparison Groups

Choose comparison group names that clearly indicate what's being compared:

```csharp
[SailfishComparison("DatabaseQueries")]     // Good
[SailfishComparison("SerializationMethods")] // Good
[SailfishComparison("Group1")]               // Poor
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

1. **Verify group names**: Ensure methods use identical group names (case-sensitive)
2. **Check method count**: Need at least 2 methods in a group for comparisons
3. **Confirm attributes**: Both `[SailfishMethod]` and `[SailfishComparison]` required

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
