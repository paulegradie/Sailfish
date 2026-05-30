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
    [SailfishMethod(ComparisonGroup = "Algorithms", IsBaseline = true)]
    public void QuickSort() { /* implementation */ }

    [SailfishMethod(ComparisonGroup = "Algorithms")]
    public void BubbleSort() { /* implementation */ }

    [SailfishMethod]
    public void RegularMethod() { /* implementation */ }
}
```

## Markdown Structure

The session-consolidated markdown file is organized as follows:

### Section 1: Session Header

```markdown
# 📊 Test Session Results

**Generated:** 2025-08-03 10:30:00 UTC
**Session ID:** abc12345
**Total Test Classes:** 1
**Total Test Cases:** 3
```

**Fields:**
- **Generated**: When the test session completed (UTC)
- **Session ID**: Unique identifier for the test session
- **Total Test Classes**: Number of test classes with `[WriteToMarkdown]` in the session
- **Total Test Cases**: Total number of test cases (methods × variable combinations) executed

The header is followed by optional `## 🏥 Environment Health Check` and `## 🔁 Reproducibility Summary` sections — see below.

### Section 2: Per-Group Comparison Sections

For each `(class, ComparisonGroup)` with at least two methods, the file emits an `## 🔬 Comparison Group: {GroupName} ({ClassName})` section. The class name is included so same-named groups in different classes are reported separately.

The section then contains one of:

- **Baseline mode** (exactly one method in the group sets `IsBaseline = true`): a `### Baseline Comparison` section with one row per contender. Ratio is contender/baseline, with a 95% CI and BH-FDR q-value.
- **N×N mode** (no baseline): a `### Performance Comparison Matrix` — every pair of methods appears as a cell with ratio (col / row), 95% CI, and q-value.

Either layout is followed by a `### Detailed Results` table:

```markdown
| Method | Mean Time | Median Time | Sample Size | Status |
|--------|-----------|-------------|-------------|--------|
| QuickSort | 2.100ms | 2.000ms | 100 | ✅ Success |
| BubbleSort | 45.200ms | 44.100ms | 100 | ✅ Success |
```

**Columns:**
- **Method**: Test case display name
- **Mean Time / Median Time**: Average and median execution time (ms, 3 decimal places)
- **Sample Size**: Number of iterations after outlier filtering
- **Status**: ✅ Success or ❌ Failed

### Section 3: Individual Test Results

Methods that aren't part of a comparison group are grouped under `## 📊 Individual Test Results` using the same five-column table.

## Session-Based Consolidation

Markdown files use **session-based consolidation**, meaning:

- **Single file per session**: All test classes with `[WriteToMarkdown]` contribute to one file
- **Cross-class comparisons**: Method comparisons work across different test classes
- **Unique naming**: Files use session IDs and timestamps to prevent conflicts
- **Complete data**: All test results from the entire session are included

**Example filename**: `TestSession_abc12345_MethodComparisons_2025-08-03_10-30-00.md`

### 🏥 Environment Health Section (when enabled)

- When the Environment Health Check is enabled, the consolidated session file includes a "🏥 Environment Health Check" section near the top showing the score and the top few entries.
- Learn more: [/docs/1/environment-health](/docs/1/environment-health)


### 🧭 Reproducibility Summary (when available)

- A short summary of environment details and a link to `Manifest_*.json` is included near the top of the consolidated file when Run Settings and the manifest provider are available.
- When seeded randomized run order is enabled, the summary includes the Randomization Seed to support reproducible reruns.

- Learn more: [/docs/1/reproducibility-manifest](/docs/1/reproducibility-manifest)

### ⏱️ Timer Calibration (when enabled)

A short header summarizes the timer:
- Stopwatch Frequency (Hz) and Effective Resolution (ns)
- BaselineOverheadTicks (no‑op call baseline)
- JitterScore (0–100) and RSD%

The section is included once per session. Disable via `RunSettingsBuilder.WithTimerCalibration(false)`.



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

Each group gets its own `## 🔬 Comparison Group: {GroupName} ({ClassName})` section with its own baseline table or N×N matrix, followed by the detailed results table. The detailed table is always 5 columns (Method, Mean Time, Median Time, Sample Size, Status); ratios, CIs, and q-values live in the comparison section above it.

### Pair counts per group

| Methods in group | No-baseline (N×N) cells off-diagonal | Baseline (N−1) rows |
| --- | --- | --- |
| 2 | 2 (one each way) | 1 |
| 3 | 6 | 2 |
| 4 | 12 | 3 |
| N | N × (N−1) | N − 1 |

Baseline mode is set by adding `IsBaseline = true` to exactly one `[SailfishMethod]` in the group. The N−1 layout is generally easier to read and gives sharper q-values because the FDR adjustment is over fewer hypotheses.

### Adaptive Precision Formatting

{% callout title="Multiple comparisons correction" type="note" %}
Sailfish applies the Benjamini–Hochberg False Discovery Rate (FDR) procedure to the set of p-values within each comparison group. Consolidated outputs include the adjusted q-value alongside the 95% ratio confidence interval.
{% /callout %}
Sailfish uses adaptive precision to ensure readability:

- **Large values (>1ms)**: 4 decimal places (e.g., 45.2000)
- **Small values (<1ms)**: 6 decimal places (e.g., 0.123456)
- **Tiny values (<0.001ms)**: 8 decimal places (e.g., 0.00012345)
- **Zero values**: Simple "0"

### Mixed Test Types

The session markdown includes both comparison methods (under per-group sections) and ungrouped methods (under `## 📊 Individual Test Results`). All tables share the same 5-column shape:

```markdown
| Method | Mean Time | Median Time | Sample Size | Status |
|--------|-----------|-------------|-------------|--------|
| RegularMethod        | 1.000ms  | 1.000ms  | 100 | ✅ Success |
| AnotherRegularMethod | 1.100ms  | 1.000ms  | 100 | ✅ Success |
```

For per-test CI95/CI99 margins of error or standard deviation, consult the per-class tracking CSV or the [Reproducibility Manifest](/docs/1/reproducibility-manifest).

## Best Practices

### 1. Organize Your Tests

Use meaningful test class and method names since they appear in the markdown:

```csharp
[WriteToMarkdown]
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

1. **Verify group names**: Ensure methods use identical `ComparisonGroup` values (case-sensitive)
2. **Check method count**: Need at least 2 methods in a group for comparisons (SF1302 warns at build time)
3. **At most one baseline**: SF1301 errors at build time if more than one method in a group sets `IsBaseline = true`

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

