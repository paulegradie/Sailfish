---
title: Method Comparisons
---

## Introduction

**Method comparison is the default behavior** for every `[Sailfish]` class. Each `[SailfishMethod]` in the class automatically joins an implicit class-wide *comparison group*, and Sailfish reports how those methods perform relative to each other.

You don't need any extra attributes for the common case — define a class, add some methods, and you get a comparison.

When a comparison group has at least two methods, Sailfish:
- Computes a ratio (and a 95% confidence interval on that ratio) between methods.
- Adjusts p-values across the group with the Benjamini–Hochberg FDR procedure and reports a q-value.
- Labels each comparison **Improved**, **Slower**, or **Similar** against the configured `SailDiffSettings.Alpha` (default `0.05`).
- Emits the comparison to the consolidated session Markdown and CSV when `[WriteToMarkdown]` / `[WriteToCsv]` are present on the class.

## Basic usage

### The default — every method is compared

```csharp
[WriteToMarkdown]
[WriteToCsv]
[Sailfish(SampleSize = 100)]
public class SortBenchmarks
{
    private List<int> _data = new();

    [SailfishGlobalSetup]
    public void Setup() => _data = Enumerable.Range(0, 1000).ToList();

    [SailfishMethod]
    public void SortWithQuickSort()
    {
        var array = _data.ToArray();
        Array.Sort(array);
    }

    [SailfishMethod]
    public void SortWithBubbleSort()
    {
        var array = _data.ToArray();
        for (var i = 0; i < array.Length - 1; i++)
            for (var j = 0; j < array.Length - i - 1; j++)
                if (array[j] > array[j + 1])
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
    }
}
```

Both methods land in the implicit class-wide group and Sailfish reports a pairwise (N×N) comparison between them.

### Adding a baseline — N−1 comparisons

When exactly one method sets `IsBaseline = true`, the output switches from an N×N matrix to an N−1 baseline-vs-contender table: each contender's ratio is reported against the baseline.

```csharp
[WriteToMarkdown]
[WriteToCsv]
[Sailfish(SampleSize = 100)]
public class SortBenchmarks
{
    private List<int> _data = new();

    [SailfishGlobalSetup]
    public void Setup() => _data = Enumerable.Range(0, 1000).ToList();

    [SailfishMethod(IsBaseline = true)]
    public void SortWithQuickSort()
    {
        var array = _data.ToArray();
        Array.Sort(array);
    }

    [SailfishMethod]
    public void SortWithBubbleSort() { /* ... */ }

    [SailfishMethod]
    public void SortWithMergeSort() { /* ... */ }
}
```

Prefer baseline mode when there's an obvious reference implementation (a current production algorithm, a "known-good" library call, etc.) — the output is shorter, easier to read at a glance, and FDR-adjusts fewer tests so individual q-values are sharper.

### Opting a class out of comparison

Some classes aren't really comparing alternatives — for example a smoke test that just times a couple of unrelated operations. Set `DisableComparison = true` on the class's `[Sailfish]` attribute to suppress the implicit group; methods then run individually with no comparison output.

```csharp
[Sailfish(DisableComparison = true)]
public class IndividualPerfTests
{
    [SailfishMethod]
    public void OperationA() { /* timed individually, no comparison */ }

    [SailfishMethod]
    public void OperationB() { /* timed individually, no comparison */ }
}
```

## Advanced — multiple named groups

For classes that benchmark several unrelated *families* of methods, set `ComparisonGroup = "name"` on `[SailfishMethod]` to peel methods off the implicit class-wide group into named ones. Multiple named groups can coexist in the same class.

```csharp
[WriteToMarkdown]
[Sailfish(SampleSize = 100)]
public class MixedBenchmarks
{
    [SailfishMethod(ComparisonGroup = "Sort", IsBaseline = true)]
    public void QuickSort() { /* ... */ }

    [SailfishMethod(ComparisonGroup = "Sort")]
    public void BubbleSort() { /* ... */ }

    [SailfishMethod(ComparisonGroup = "Hash", IsBaseline = true)]
    public void Sha256() { /* ... */ }

    [SailfishMethod(ComparisonGroup = "Hash")]
    public void Md5() { /* ... */ }
}
```

Each named group produces its own section in the consolidated outputs, and each group can independently pick zero or one baseline.

Most users never need this — the implicit class-wide group is enough for the vast majority of comparison classes.

## Rules per group

For each comparison group on a class — explicit *or* implicit — the number of methods marked `IsBaseline = true` determines the output:

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
| **SF1300** | Error | `IsBaseline = true` is invalid when the method isn't in any comparison group — i.e. its class has `DisableComparison = true` *and* the method has no explicit `ComparisonGroup`. Either set a `ComparisonGroup`, remove `DisableComparison`, or drop `IsBaseline`. |
| **SF1301** | Error | At most one method per comparison group (explicit or implicit class-wide) may set `IsBaseline = true`. Two or more is ambiguous; the runtime falls back to N×N and logs a warning if you suppress this. |
| **SF1302** | Warning | A comparison group with fewer than two methods produces no output. Either add another method, drop the explicit `ComparisonGroup`, or set `DisableComparison = true` on the class. |

## Output formats

Method comparison results are emitted in three places:

### 1. Test output window (IDE / Console)

Each method's individual descriptive statistics (mean, median, stddev, outliers) appear in the IDE Test Output window or console as it normally would for any Sailfish method. The pairwise / baseline comparison tables live in the consolidated session files.

### 2. Consolidated Markdown (`[WriteToMarkdown]`)

A single Markdown file per session containing:
- Session header (session ID, timestamp, test counts).
- Optional Environment Health and Reproducibility Summary sections.
- One section per comparison group:
  - **Implicit class-wide group**: `## 🔬 Comparisons: {ClassName}`
  - **Explicit named group**: `## 🔬 Comparison Group: {Name} ({ClassName})`
  - Each section contains either a baseline table (when one method is the baseline) or an N×N matrix.
- A `### Detailed Results` table with mean / median / sample size / status for each member.
- A `## 📊 Individual Test Results` section for any method that isn't in a comparison group (e.g. members of a `DisableComparison = true` class).

**Example filename**: `TestSession_abc12345_Results_20250803_103000.md`

### 3. Consolidated CSV (`[WriteToCsv]`)

A single CSV file per session containing:
- Session metadata.
- Individual test results — each row's `ComparisonGroup` column carries the explicit group name when set, the class name for the implicit group, or empty when the method isn't compared.
- A `# Method Comparisons` section with one row per comparison pair — N−1 rows in baseline mode, N×(N−1)/2 rows in N×N mode.

**Example filename**: `TestSession_abc12345_Results_20250803_103000.csv`

{% callout title="See also" type="note" %}
Format details and the exact column / section layout:
- [Markdown Output](/docs/1/markdown-output)
- [CSV Output](/docs/1/csv-output)
{% /callout %}

## Understanding the results

### Baseline mode — example output

For the `SortBenchmarks` class above (implicit group, quicksort baseline, bubble + merge sort as contenders):

```
## 🔬 Comparisons: SortBenchmarks

### Baseline Comparison

### 📐 Baseline-vs-Contender (baseline = `SortWithQuickSort`, q-values via BH-FDR, α=0.05)

| Method                            | Mean     | Ratio vs Baseline | 95% CI                | q-value | Label  |
|-----------------------------------|----------|-------------------|-----------------------|---------|--------|
| `SortWithQuickSort` _(baseline)_  | 0.005ms  | —                 | —                     | —       | —      |
| `SortWithMergeSort`               | 0.018ms  | 3.600x            | [3.291–3.937]         | 4.1e-09 | Slower |
| `SortWithBubbleSort`              | 1.730ms  | 346.000x          | [298.412–401.213]     | 1.2e-12 | Slower |

_Ratio is contender/baseline. 'Improved' means significantly faster than baseline; 'Slower' significantly slower; 'Similar' not significant after FDR._

### Detailed Results

| Method              | Mean Time | Median Time | Sample Size | Status     |
|---------------------|-----------|-------------|-------------|------------|
| SortWithQuickSort   | 0.005ms   | 0.005ms     | 100         | ✅ Success |
| SortWithMergeSort   | 0.018ms   | 0.017ms     | 100         | ✅ Success |
| SortWithBubbleSort  | 1.730ms   | 1.666ms     | 100         | ✅ Success |
```

### N×N mode — example output

The same `SortBenchmarks` class without `IsBaseline`:

```
## 🔬 Comparisons: SortBenchmarks

### Performance Comparison Matrix

### 🔢 NxN Comparison Matrix (q-values via BH-FDR, α=0.05)

| Method              | SortWithQuickSort                             | SortWithMergeSort                             | SortWithBubbleSort                            |
|---------------------|-----------------------------------------------|-----------------------------------------------|-----------------------------------------------|
| SortWithQuickSort   | —                                             | 3.600x [3.291–3.937] q=4.1e-09 Slower         | 346.000x [298.412–401.213] q=1.2e-12 Slower   |
| SortWithMergeSort   | 0.278x [0.254–0.304] q=4.1e-09 Improved       | —                                             | 96.111x [82.341–112.182] q=3.7e-11 Slower     |
| SortWithBubbleSort  | 0.003x [0.002–0.003] q=1.2e-12 Improved       | 0.010x [0.009–0.012] q=3.7e-11 Improved       | —                                             |

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

## Best practices

### 1. Use a baseline when one exists

If there's an obvious reference (current production code, a library call, an algorithm you're trying to beat), nominate it with `IsBaseline = true`. Output is shorter and the FDR adjustment is tighter — N−1 hypotheses vs. N×(N−1)/2.

### 2. Use meaningful group names — when you need them

The implicit class-wide group is named after the class itself, which is usually all the context the output needs. Reach for explicit `ComparisonGroup` only when a single class has multiple distinct comparisons to make.

```csharp
[SailfishMethod(ComparisonGroup = "DatabaseQueries")]      // Good
[SailfishMethod(ComparisonGroup = "SerializationMethods")] // Good
[SailfishMethod(ComparisonGroup = "Group1")]               // Poor
```

### 3. Ensure fair comparisons

Compared methods should be testing equivalent functionality. The class itself is your natural scoping mechanism — if two methods don't belong in the same comparison, they probably don't belong in the same class.

```csharp
// Good: every method in the class does the same kind of thing
[Sailfish]
public class SortBenchmarks
{
    [SailfishMethod(IsBaseline = true)]
    public void QuickSort() { /* sorts _data */ }

    [SailfishMethod]
    public void BubbleSort() { /* sorts _data */ }
}

// Poor: unrelated operations in the same class — split into two classes,
// or set DisableComparison = true if you really want them together.
[Sailfish]
public class MixedOperations
{
    [SailfishMethod]
    public void SortData() { /* sorts data */ }

    [SailfishMethod]
    public void SearchData() { /* searches data */ }
}
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

[SailfishMethod(IsBaseline = true)]
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

1. **At least two methods**: a lone method on a class produces no comparison (SF1302).
2. **Class is `[Sailfish]`**: the implicit class-wide group only forms on classes decorated with `[Sailfish]`. Methods on undecorated types are ignored entirely.
3. **`DisableComparison = true`**: check whether the class — or an explicit group you set on a method — has opted out.
4. **`[WriteToMarkdown]` / `[WriteToCsv]` on the class**: the consolidated session files are only generated when one of those attributes is present.
5. **Run more than one method**: comparison output only appears when at least two members of the group execute in the same session.

### SF1300 error: "IsBaseline=true on a method that isn't in any comparison group"

You set `IsBaseline = true` on a method whose class has `DisableComparison = true` *and* you didn't set an explicit `ComparisonGroup`. The method is therefore in no group, so the baseline flag has nothing to apply to. Either:
- Set `ComparisonGroup = "..."` on the method, or
- Remove `DisableComparison = true` from the class, or
- Drop `IsBaseline` if the method shouldn't be a baseline.

### SF1301 error: "Only one IsBaseline per comparison group is allowed"

Two or more methods in the same comparison group (explicit or implicit) set `IsBaseline = true`. Pick one. If you really want N×N output, remove `IsBaseline` from all of them.

### SF1302 warning: "Comparison group needs at least two methods"

A comparison group has only one method. Add a peer, drop the explicit `ComparisonGroup`, or set `DisableComparison = true` on the class if it isn't really doing a comparison.

### Unexpected results

1. **Check test isolation**: ensure methods don't interfere with each other (shared mutable state, leftover side-effects).
2. **Verify data consistency**: all methods should work with equivalent input.
3. **Increase sample size**: small differences need more samples to resolve.
4. **Check outliers**: extreme noise can dominate small effects even after outlier filtering.

## Complete examples

Runnable examples live in the repository:

- [`MethodComparisonExample.cs`](https://github.com/paulegradie/Sailfish/blob/main/source/PerformanceTests/ExamplePerformanceTests/MethodComparisonExample.cs) — the simplest form: an implicit class-wide group with one baseline.
- [`MultiGroupComparisonExample.cs`](https://github.com/paulegradie/Sailfish/blob/main/source/PerformanceTests/ExamplePerformanceTests/MultiGroupComparisonExample.cs) — multiple distinct named groups in one class via explicit `ComparisonGroup`.
- [`DisabledComparisonExample.cs`](https://github.com/paulegradie/Sailfish/blob/main/source/PerformanceTests/ExamplePerformanceTests/DisabledComparisonExample.cs) — opting a class out of comparison via `DisableComparison = true`.
