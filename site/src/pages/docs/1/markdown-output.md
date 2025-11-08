---
title: Markdown Output Format
---

## Introduction

Sailfish can generate comprehensive markdown files containing both individual test results and method comparison data using the `[WriteToMarkdown]` attribute. These files are GitHub-compatible and perfect for documentation, code reviews, and performance tracking.

## Basic Usage

Apply the `[WriteToMarkdown]` attribute to any test class:

```csharp
[WriteToMarkdown]
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

## Markdown Structure

The generated markdown files use a well-organized, multi-section format:

### Section 1: Session Metadata

```markdown
# Performance Test Results
**Session ID:** abc12345
**Timestamp:** 2025-08-03T10:30:00Z
**Total Classes:** 1
**Total Tests:** 3
```

**Fields:**
- **Session ID**: Unique identifier for the test session
- **Timestamp**: When the test session completed (UTC)
- **Total Classes**: Number of test classes with `[WriteToMarkdown]` in the session
- **Total Tests**: Total number of test methods executed

### Section 2: Individual Test Results

#### PerformanceTest

| Method | Mean (ms) | Median (ms) | StdDev (N=100) | CI95 MOE | CI99 MOE | Status |
|--------|-----------|-------------|----------------|----------|----------|--------|
| BubbleSort | 45.2000 | 44.1000 | 3.1000 | ±1.2345 | ±1.6789 | ✅ Success |
| QuickSort | 2.1000 | 2.0000 | 0.3000 | ±0.1234 | ±0.2345 | ✅ Success |
| RegularMethod | 1.0000 | 1.0000 | 0.1000 | ±0.0500 | ±0.0800 | ✅ Success |

**Columns:**
- **Method**: Name of the test method
- **Mean (ms)**: Average execution time in milliseconds
- **Median (ms)**: Median execution time in milliseconds
- **StdDev (N=X)**: Standard deviation with sample size indicator
- **CI95 MOE**: Margin of error at 95% confidence (±ms)
- **CI99 MOE**: Margin of error at 99% confidence (±ms)
- **Status**: Test execution status with emoji indicator

### Section 3: Method Comparison Matrices

#### Comparison Group: Algorithms

| Method 1 | Method 2 | Mean 1 (ms) | Mean 2 (ms) | Ratio | P-Value | Significance |
|----------|----------|-------------|-------------|-------|---------|--------------|
| BubbleSort | QuickSort | 45.2000 | 2.1000 | 21.5x slower | < 0.001 | ⚠️ Regressed |

**Columns:**
- **Method 1 / Method 2**: Methods being compared
- **Mean 1 / Mean 2**: Mean execution times for each method
- **Ratio**: Performance relationship (e.g., "21.5x slower", "2.3x faster")
- **P-Value**: Statistical significance of the difference
- **Significance**: Visual indicator of performance change
  - ✅ **Improved**: Method 1 is significantly faster
  - ⚠️ **Regressed**: Method 1 is significantly slower
  - ➖ **No Change**: No statistically significant difference

## Session-Based Consolidation

Markdown files use **session-based consolidation**, meaning:

- **Single file per session**: All test classes with `[WriteToMarkdown]` contribute to one file
- **Cross-class comparisons**: Method comparisons work across different test classes
- **Unique naming**: Files use session IDs and timestamps to prevent conflicts
- **Complete data**: All test results from the entire session are included

**Example filename**: `TestSession_abc12345_Results_20250803_103000.md`

### 🏥 Environment Health Section (when enabled)

- When the Environment Health Check is enabled, the consolidated session file includes a "🏥 Environment Health Check" section near the top showing the score and the top few entries.
- Learn more: [/docs/1/environment-health](/docs/1/environment-health)


## GitHub Integration

The markdown format is designed for seamless GitHub integration:

### 1. Commit to Repository

```bash
git add TestSession_*.md
git commit -m "Add performance test results"
git push
```

### 2. View in Pull Requests

- **Rendered tables**: GitHub automatically renders markdown tables
- **Emoji support**: Status indicators display correctly
- **Diff-friendly**: Changes between test runs are easy to spot
- **Searchable**: Full-text search across all results

### 3. Link in Documentation

```markdown
See [latest performance results](./TestSession_abc12345_Results_20250803_103000.md)
```

## Advanced Features

### Multiple Comparison Groups

When you have multiple comparison groups, each generates its own comparison matrix:

#### Comparison Group: StringOperations

| Method 1 | Method 2 | Mean 1 (ms) | Mean 2 (ms) | Ratio | P-Value | Significance |
|----------|----------|-------------|-------------|-------|---------|--------------|
| StringConcat | StringBuilder | 15.2000 | 8.1000 | 1.9x slower | < 0.001 | ⚠️ Regressed |
| StringConcat | StringInterpolation | 15.2000 | 12.3000 | 1.2x slower | 0.023 | ⚠️ Regressed |
| StringBuilder | StringInterpolation | 8.1000 | 12.3000 | 1.5x faster | 0.001 | ✅ Improved |

#### Comparison Group: Collections

| Method 1 | Method 2 | Mean 1 (ms) | Mean 2 (ms) | Ratio | P-Value | Significance |
|----------|----------|-------------|-------------|-------|---------|--------------|
| ListIteration | ArrayIteration | 5.4000 | 3.2000 | 1.7x slower | < 0.001 | ⚠️ Regressed |

### N×N Comparison Matrices

For groups with multiple methods, all pairwise comparisons are included:

- **2 methods**: 1 comparison
- **3 methods**: 3 comparisons (A vs B, A vs C, B vs C)
- **4 methods**: 6 comparisons
- **N methods**: N×(N-1)/2 comparisons

### Adaptive Precision Formatting

Sailfish uses adaptive precision to ensure readability:

- **Large values (>1ms)**: 4 decimal places (e.g., 45.2000)
- **Small values (<1ms)**: 6 decimal places (e.g., 0.123456)
- **Tiny values (<0.001ms)**: 8 decimal places (e.g., 0.00012345)
- **Zero values**: Simple "0"

### Mixed Test Types

The markdown includes both comparison and regular methods:

#### MyTest

| Method | Mean (ms) | Median (ms) | StdDev (N=100) | CI95 MOE | CI99 MOE | Status |
|--------|-----------|-------------|----------------|----------|----------|--------|
| ComparisonMethod1 | 10.5000 | 9.8000 | 1.2000 | ±0.4567 | ±0.6789 | ✅ Success |
| ComparisonMethod2 | 8.3000 | 8.1000 | 0.9000 | ±0.3456 | ±0.5123 | ✅ Success |
| RegularMethod | 1.0000 | 1.0000 | 0.1000 | ±0.0500 | ±0.0800 | ✅ Success |
| AnotherRegularMethod | 1.1000 | 1.0000 | 0.1000 | ±0.0500 | ±0.0800 | ✅ Success |

## Best Practices

### 1. Organize Your Tests

Use meaningful test class and method names since they appear in the markdown:

```csharp
[WriteToMarkdown]
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

### 4. Combine with CSV

Use both output formats for comprehensive reporting:

```csharp
[WriteToMarkdown]  // Human-readable reports
[WriteToCsv]       // Data analysis
[Sailfish]
public class ComprehensiveTest { }
```

### 5. Version Control Integration

Add markdown files to version control for historical tracking:

```gitignore
# .gitignore - Include performance results
!TestSession_*.md
```

## Troubleshooting

### Empty Markdown Files

If markdown files are empty or missing:

1. **Check attribute placement**: Ensure `[WriteToMarkdown]` is on the test class, not methods
2. **Verify test execution**: Markdown is only generated after successful test completion
3. **Check output directory**: Verify the configured output directory exists and is writable

### Missing Comparisons

If method comparisons are missing from the markdown:

1. **Verify group names**: Ensure methods use identical group names (case-sensitive)
2. **Check method count**: Need at least 2 methods in a group for comparisons
3. **Confirm attributes**: Both `[SailfishMethod]` and `[SailfishComparison]` required

### GitHub Rendering Issues

If GitHub doesn't render the markdown correctly:

1. **Check file encoding**: Ensure markdown is saved as UTF-8
2. **Verify table syntax**: Ensure proper pipe (`|`) alignment
3. **Test locally**: Preview markdown in VS Code or other editor first

## Integration Examples

### CI/CD Pipeline

```yaml
- name: Run Performance Tests
  run: dotnet test --logger "console;verbosity=detailed"

- name: Upload Markdown Results
  uses: actions/upload-artifact@v3
  with:
    name: performance-results
    path: "**/TestSession_*.md"

- name: Comment on PR
  uses: actions/github-script@v6
  with:
    script: |
      const fs = require('fs');
      const markdown = fs.readFileSync('TestSession_latest.md', 'utf8');
      github.rest.issues.createComment({
        issue_number: context.issue.number,
        owner: context.repo.owner,
        repo: context.repo.repo,
        body: markdown
      });
```

### Performance Tracking

```csharp
// Compare current results with baseline
var currentResults = File.ReadAllText("TestSession_current.md");
var baselineResults = File.ReadAllText("TestSession_baseline.md");

// Parse and analyze differences
if (HasPerformanceRegression(currentResults, baselineResults))
{
    SendAlert("Performance regression detected!");
}
```

### Documentation Generation

```csharp
// Automatically update documentation with latest results
var latestResults = Directory.GetFiles("./performance-results", "TestSession_*.md")
    .OrderByDescending(f => File.GetCreationTime(f))
    .First();

File.Copy(latestResults, "./docs/performance/latest-results.md", overwrite: true);
```

