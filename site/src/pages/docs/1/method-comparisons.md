---
title: Method Comparisons
---

## Introduction

**Method Comparisons** let you put two or more methods in a named *comparison group* on a single test class, then automatically have Sailfish report how they perform relative to each other. Comparison is configured directly on `[SailfishMethod]` via two properties:

- `ComparisonGroup = "..."` opts a method into a named group. Two or more methods in the same group on the same class are compared.
- `IsBaseline = true` (at most one method per group) switches the group from the default N×N matrix to N−1 baseline-vs-contender rows.

When a group has at least two methods, Sailfish:
- Computes a ratio (and a 95% confidence interval on that ratio) between methods.
- Adjusts p-values across the group with the Benjamini–Hochberg FDR procedure and reports a q-value.
- Labels each comparison **Improved**, **Slower**, or **Similar** against the configured `SailDiffSettings.Alpha` (default `0.05`).
- Emits the comparison to the consolidated session Markdown and CSV when `[WriteToMarkdown]` / `[WriteToCsv]` are present on the class.

## Basic Usage

### Default — N×N matrix (no baseline)

When you don't nominate a baseline, every pair of methods in the group is compared and the markdown shows a full N×N matrix.

```csharp
[WriteToMarkdown]
[WriteToCsv]
[Sailfish(SampleSize = 100)]
public class SumComparison
{
    private List<int> _data = new();

    [SailfishGlobalSetup]
    public void Setup() => _data = Enumerable.Range(0, 1000).ToList();

    [SailfishMethod(ComparisonGroup = "SumCalculation")]
    public void CalculateSumWithLinq()
    {
        var sum = _data.Sum();
    }

    [SailfishMethod(ComparisonGroup = "SumCalculation")]
    public void CalculateSumWithLoop()
    {
        var sum = 0;
        for (var i = 0; i < _data.Count; i++) sum += _data[i];
    }
}
```

With a third method in the group, you'd get three pairwise comparisons; with N methods, N×(N−1)/2.

### Baseline mode — N−1 comparisons

When exactly one method in the group sets `IsBaseline = true`, the output collapses to N−1 rows: each contender is reported as a ratio against the baseline.

```csharp
[WriteToMarkdown]
[WriteToCsv]
[Sailfish(SampleSize = 100)]
public class SortingComparison
{
    private List<int> _data = new();

    [SailfishGlobalSetup]
    public void Setup() => _data = Enumerable.Range(0, 1000).ToList();

    [SailfishMethod(ComparisonGroup = "SortingAlgorithm", IsBaseline = true)]
    public void SortWithQuickSort()
    {
        var array = _data.ToArray();
        Array.Sort(array);
    }

    [SailfishMethod(ComparisonGroup = "SortingAlgorithm")]
    public void SortWithBubbleSort()
    {
        var array = _data.ToArray();
        for (var i = 0; i < array.Length - 1; i++)
            for (var j = 0; j < array.Length - i - 1; j++)
                if (array[j] > array[j + 1])
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
    }

    [SailfishMethod(ComparisonGroup = "SortingAlgorithm")]
    public void SortWithOtherSort()
    {
        Thread.Sleep(10);
    }
}
```

Prefer baseline mode when there's an obvious reference implementation (a current production algorithm, a "known-good" library call, etc.) — the output is shorter, easier to read at a glance, and FDR-adjusts fewer tests so individual q-values are sharper.

### Mixing comparison and non-comparison methods

A test class can mix methods with and without `ComparisonGroup`. Methods without it run as ordinary Sailfish methods and never appear in any comparison section.

```csharp
[SailfishMethod(ComparisonGroup = "SortingAlgorithm", IsBaseline = true)]
public void SortWithQuickSort() { /* ... */ }

[SailfishMethod(ComparisonGroup = "SortingAlgorithm")]
public void SortWithBubbleSort() { /* ... */ }

[SailfishMethod]
public void RegularMethod() { /* never compared */ }
```

### Multiple groups in one class

A class can declare any number of independent groups:

```csharp
[SailfishMethod(ComparisonGroup = "Serialization", IsBaseline = true)]
public void SerializeWithSystemTextJson() { /* ... */ }

[SailfishMethod(ComparisonGroup = "Serialization")]
public void SerializeWithNewtonsoft() { /* ... */ }

[SailfishMethod(ComparisonGroup = "Collections")]
public void ListIteration() { /* ... */ }

[SailfishMethod(ComparisonGroup = "Collections")]
public void ArrayIteration() { /* ... */ }
```

Each group produces its own section in the consolidated outputs.

## Rules per group

For each `(class, ComparisonGroup)` pair, the number of methods marked `IsBaseline = true` determines the output:

| Baselines in the group | Behavior |
| --- | --- |
| **0** | N×N matrix — every pair compared. |
| **1** | N−1 baseline-vs-contender rows. |
| **2+** | Build error (SF1301). Runtime falls back to N×N and emits a warning. |

A group with only one method produces no comparison (the SF1302 analyzer warns at build time).

{% callout title="Per-class scoping" type="note" %}
Comparison groups are scoped **per test class**, not across the whole session. Two different test classes can each define `ComparisonGroup = "Sort"` and they will be reported as independent groups. The consolidated Markdown header includes the class name to disambiguate: `## 🔬 Comparison Group: Sort (ClassNameA)` and `## 🔬 Comparison Group: Sort (ClassNameB)`. The CSV row's `ComparisonGroup` column emits just the group name — the class is identifiable from the surrounding row context.
{% /callout %}

## Build-time checks (analyzers)

The Sailfish analyzers catch the most common mistakes at compile time:

| ID | Severity | Rule |
| --- | --- | --- |
| **SF1300** | Error | `IsBaseline = true` requires `ComparisonGroup` to be set on the same `[SailfishMethod]` attribute. A baseline outside a group is meaningless. |
| **SF1301** | Error | At most one method per `(class, ComparisonGroup)` may set `IsBaseline = true`. Two or more is ambiguous; the runtime falls back to N×N and logs a warning if you suppress this. |
| **SF1302** | Warning | A `ComparisonGroup` with fewer than two methods produces no output. Either add another method or remove `ComparisonGroup`. |

## Output Formats

Method comparison results are emitted in three places:

### 1. Test output window (IDE / Console)

Each method's individual descriptive statistics (mean, median, stddev, outliers) appear in the IDE Test Output window or console as it normally would for any Sailfish method. The pairwise / baseline comparison tables live in the consolidated session files.

### 2. Consolidated Markdown (`[WriteToMarkdown]`)

A single Markdown file per session containing:
- Session header (session ID, timestamp, test counts).
- Optional Environment Health and Reproducibility Summary sections.
- One `## 🔬 Comparison Group: GroupName (ClassName)` section per group, containing either a baseline table (when one method is the baseline) or an N×N matrix.
- A `### Detailed Results` table with mean / median / sample size / status for each member.
- A `## 📊 Individual Test Results` section for any method that wasn't in a comparison group.

**Example filename**: `TestSession_abc12345_Results_20250803_103000.md`

### 3. Consolidated CSV (`[WriteToCsv]`)

A single CSV file per session containing:
- Session metadata.
- Individual test results (all methods, with their `ComparisonGroup` column populated when applicable).
- A `# Method Comparisons` section with one row per comparison — N−1 rows in baseline mode, N×(N−1)/2 rows in N×N mode.

**Example filename**: `TestSession_abc12345_Results_20250803_103000.csv`

{% callout title="See also" type="note" %}
Format details and the exact column / section layout:
- [Markdown Output](/docs/1/markdown-output)
- [CSV Output](/docs/1/csv-output)
{% /callout %}

## Understanding the Results

### Baseline mode — example output

For the `SortingAlgorithm` group above (quicksort baseline, plus bubble sort and a slow sleeper):

```
## 🔬 Comparison Group: SortingAlgorithm (SortingComparison)

### Baseline Comparison

### 📐 Baseline-vs-Contender (baseline = `SortWithQuickSort`, q-values via BH-FDR, α=0.05)

| Method                            | Mean     | Ratio vs Baseline | 95% CI                | q-value | Label  |
|-----------------------------------|----------|-------------------|-----------------------|---------|--------|
| `SortWithQuickSort` _(baseline)_  | 0.005ms  | —                 | —                     | —       | —      |
| `SortWithBubbleSort`              | 1.730ms  | 346.000x          | [298.412–401.213]     | 1.2e-12 | Slower |
| `SortWithOtherSort`               | 15.622ms | 3124.400x         | [2901.011–3365.121]   | 8.4e-15 | Slower |

_Ratio is contender/baseline. 'Improved' means significantly faster than baseline; 'Slower' significantly slower; 'Similar' not significant after FDR._

### Detailed Results

| Method              | Mean Time | Median Time | Sample Size | Status     |
|---------------------|-----------|-------------|-------------|------------|
| SortWithQuickSort   | 0.005ms   | 0.005ms     | 100         | ✅ Success |
| SortWithBubbleSort  | 1.730ms   | 1.666ms     | 100         | ✅ Success |
| SortWithOtherSort   | 15.622ms  | 15.530ms    | 100         | ✅ Success |
```

### N×N mode — example output

For the `SumCalculation` group above (no baseline, two methods):

```
## 🔬 Comparison Group: SumCalculation (SumComparison)

### Performance Comparison Matrix

### 🔢 NxN Comparison Matrix (q-values via BH-FDR, α=0.05)

| Method                | CalculateSumWithLinq                  | CalculateSumWithLoop                  |
|-----------------------|---------------------------------------|---------------------------------------|
| CalculateSumWithLinq  | —                                     | 0.987x [0.951–1.024] q=0.512 Similar  |
| CalculateSumWithLoop  | 1.013x [0.977–1.052] q=0.512 Similar  | —                                     |

_Cell value is ratio vs. row (col/row). CI is 95% on ratio. 'Improved' means significantly faster; 'Slower' significantly slower; 'Similar' not significant after FDR._
```

### Reading the labels

- **Improved**: significantly faster than its reference (the baseline in N−1 mode, the row method in N×N mode) after the FDR adjustment.
- **Slower**: significantly slower than its reference after the FDR adjustment.
- **Similar**: not significant at the configured `Alpha` (default 0.05) after FDR — either truly indistinguishable, or the sample size is too small to resolve a real difference.

The 95% confidence interval on the ratio is the most direct measure of magnitude. If the interval doesn't cross 1.0 cleanly, the difference is meaningful at the chosen α.

## Failure handling

If a method in a comparison group throws, Sailfish publishes a normal failed `TestOutcome` for that case immediately and **excludes it from the comparison batch**:

- The exception surfaces in the IDE Test Explorer as a failure, with the stack trace attached.
- Sibling members are not blocked — the comparison batch readiness check only counts surviving members.
- The comparison is computed across the survivors. If fewer than two members survive, no comparison is produced (a short note is emitted to the consolidated outputs).

This is why a partially-failing group still reports useful comparisons rather than silently hanging.

## Best Practices

### 1. Use a baseline when one exists

If there's an obvious reference (current production code, a library call, an algorithm you're trying to beat), nominate it with `IsBaseline = true`. Output is shorter and the FDR adjustment is tighter — N−1 hypotheses vs. N×(N−1)/2.

### 2. Use meaningful group names

```csharp
[SailfishMethod(ComparisonGroup = "DatabaseQueries")]      // Good
[SailfishMethod(ComparisonGroup = "SerializationMethods")] // Good
[SailfishMethod(ComparisonGroup = "Group1")]               // Poor
```

### 3. Ensure fair comparisons

Compared methods should be testing equivalent functionality:

```csharp
// Good: all members of the group do the same thing
[SailfishMethod(ComparisonGroup = "SortingAlgorithms", IsBaseline = true)]
public void QuickSort() { /* sorts _data */ }

[SailfishMethod(ComparisonGroup = "SortingAlgorithms")]
public void BubbleSort() { /* sorts _data */ }

// Poor: the two members aren't comparable
[SailfishMethod(ComparisonGroup = "Mixed")]
public void SortData() { /* sorts data */ }

[SailfishMethod(ComparisonGroup = "Mixed")]
public void SearchData() { /* searches data */ }
```

### 4. Sample sizes large enough to resolve the effect you care about

{% callout title="Tip: Adaptive Sampling" type="note" %}
Instead of guessing a fixed `SampleSize`, enable [Adaptive Sampling](/docs/1/adaptive-sampling). Sailfish stops once results are statistically stable (using coefficient of variation and confidence interval width thresholds), which often shortens runtime while preserving rigor. Opt in per class via `[Sailfish]` or set a global policy with `RunSettingsBuilder`.
{% /callout %}

```csharp
[Sailfish(SampleSize = 100)] // good default for most comparisons
public class PerformanceComparison
{
    // Methods with small performance differences need larger samples;
    // methods with large differences can use smaller samples.
}
```

### 5. Isolate methods from each other

Each method should be independent. Mutating shared state from one method into the next will silently bias comparisons.

```csharp
[SailfishGlobalSetup]
public void Setup() => _data = GenerateTestData();

[SailfishMethod(ComparisonGroup = "Algorithms", IsBaseline = true)]
public void Method1()
{
    var local = _data.ToArray(); // work on a copy
    // ...
}
```

## Configuring the significance threshold

You can configure the alpha used by the method-comparison output via `.sailfish.json`:

```json
{
  "SailDiffSettings": {
    "Alpha": 0.05,
    "Disabled": false
  }
}
```

The Improved / Slower / Similar labels are decided against the configured `Alpha` after the BH-FDR adjustment. The N×N matrix and the baseline-vs-contender table both use that same `Alpha` for per-pair significance, and the reported ratio confidence intervals follow the matching `1 − Alpha` level — no hardcoded `0.05` in the formatters.

{% callout title="TestType is not honoured here" type="note" %}
`SailDiffSettings.TestType` controls the test used by [historical SailDiff comparisons](/docs/2/saildiff), not method comparisons. The method-comparison handlers always compute p-values from a Welch-style log-ratio approximation (Student-t CDF on the log-scale standard error), regardless of what `TestType` is set to in `.sailfish.json`. Only `Alpha` is read by these output paths.
{% /callout %}

## Troubleshooting

### No comparison section in the output

1. **At least two methods in the group**: a lone method in a group produces no comparison (SF1302).
2. **Identical group names**: `ComparisonGroup` is case-sensitive — `"sort"` and `"Sort"` are different groups.
3. **`[WriteToMarkdown]` / `[WriteToCsv]` on the class**: the consolidated session files are only generated when one of those attributes is present.
4. **Run more than one method**: comparison output only appears when at least two members of the group execute in the same session.

### SF1301 error: "Only one IsBaseline per ComparisonGroup is allowed"

Two or more methods in the same group set `IsBaseline = true`. Pick one. If you really want N×N output, remove `IsBaseline` from all of them.

### SF1300 error: "IsBaseline=true requires a ComparisonGroup"

You set `IsBaseline = true` but forgot `ComparisonGroup`. Either add the group name or remove `IsBaseline`.

### Unexpected results

1. **Check test isolation**: ensure methods don't interfere with each other (shared mutable state, leftover side-effects).
2. **Verify data consistency**: all methods should work with equivalent input.
3. **Increase sample size**: small differences need more samples to resolve.
4. **Check outliers**: extreme noise can dominate small effects even after outlier filtering.

## Complete example

See the runnable example in the repository: [`source/PerformanceTests/ExamplePerformanceTests/MethodComparisonExample.cs`](https://github.com/paulegradie/Sailfish/blob/main/source/PerformanceTests/ExamplePerformanceTests/MethodComparisonExample.cs). It exercises both modes — a no-baseline `SumCalculation` group (N×N) and a `SortingAlgorithm` group with `SortWithQuickSort` as the baseline (N−1) — alongside ordinary non-comparison methods on the same class.
