---
title: Output Attributes
---

Sailfish prints to StdOut and writes a tracking file into the calling assemblies **bin** directory by default. You can export additional formatted documents using the following attributes.

## WriteToMarkdown

The `[WriteToMarkdown]` attribute generates consolidated markdown files containing both individual test results and method comparison data when applied to test classes.

```csharp
[WriteToMarkdown]
[Sailfish]
public class PerformanceTest
{
    [SailfishMethod]
    [SailfishComparison("Algorithms")]
    public void BubbleSort()
    {
        // Implementation
    }

    [SailfishMethod]
    [SailfishComparison("Algorithms")]
    public void QuickSort()
    {
        // Implementation
    }

    [SailfishMethod]
    public void RegularMethod()
    {
        // Implementation
    }
}
```

### Markdown Output Features

- **Session-based consolidation**: Single markdown file per test session
- **Individual test results**: Detailed statistics for each method
- **Method comparison matrices**: N×N comparisons between methods in the same comparison group
- **Statistical analysis**: P-values, significance testing, and performance ratios
- **Organized sections**: Clear separation between different types of data
- **Multiple confidence intervals**: Displays 95% and 99% by default with adaptive precision

**Example filename**: `TestSession_abc12345_Results_20250803_103000.md`

## WriteToCsv

The `[WriteToCsv]` attribute generates consolidated CSV files containing both individual test results and method comparison data in a structured, Excel-friendly format.

```csharp
[WriteToCsv]
[Sailfish]
public class PerformanceTest
{
    [SailfishMethod]
    [SailfishComparison("Algorithms")]
    public void BubbleSort()
    {
        // Implementation
    }

    [SailfishMethod]
    [SailfishComparison("Algorithms")]
    public void QuickSort()
    {
        // Implementation
    }

    [SailfishMethod]
    public void RegularMethod()
    {
        // Implementation
    }
}
```

### CSV Output Features

- **Multi-section format**: Session metadata, individual results, and method comparisons
- **Excel-compatible**: Easy to import and analyze in spreadsheet applications
- **Session-based consolidation**: Single CSV file per test session
- **Comprehensive data**: All test metrics and comparison results in tabular format
- **Comment-friendly**: Section headers using `#` comments for clear organization
- **Multiple confidence intervals**: Includes CI95_MOE and CI99_MOE columns for margin-of-error at 95% and 99% confidence


**Example filename**: `TestSession_abc12345_Results_20250803_103000.csv`

### CSV Structure

```csv
# Session Metadata
SessionId,Timestamp,TotalClasses,TotalTests
abc12345,2025-08-03T10:30:00Z,1,6

# Individual Test Results
TestClass,TestMethod,MeanTime,MedianTime,StdDev,SampleSize,ComparisonGroup,Status
PerformanceTest,BubbleSort,45.200,44.100,3.100,100,Algorithms,Success
PerformanceTest,QuickSort,2.100,2.000,0.300,100,Algorithms,Success
PerformanceTest,RegularMethod,1.000,1.000,0.100,100,,Success

# Method Comparisons
ComparisonGroup,Method1,Method2,Method1Mean,Method2Mean,PerformanceRatio,ChangeDescription
Algorithms,BubbleSort,QuickSort,45.200,2.100,21.5x slower,Regressed
```

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

### 2. Organize Comparison Groups
Use meaningful comparison group names that clearly indicate what's being compared:

```csharp
[SailfishComparison("DatabaseQueries")]     // Good
[SailfishComparison("SerializationMethods")] // Good
[SailfishComparison("Group1")]               // Poor
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

**Note on Extensibility**:
Sailfish exposes notification handlers that you can implement to customize output generation:

- `INotificationHandler<WriteMethodComparisonMarkdownNotification>` for markdown customization
- `INotificationHandler<WriteMethodComparisonCsvNotification>` for CSV customization

These handlers allow you to customize what is done with the generated content before it's written to files.