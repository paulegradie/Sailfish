---
title: SailDiff
---

## Introduction

**SailDiff** is a tool for running automated statistical testing on Sailfish performance data. It provides powerful comparison capabilities to help you understand performance changes and differences.

SailDiff operates in two main modes:

1. **Historical Comparisons**: Compare the current run against a *specific, explicitly provided* tracking file from an earlier run. Sailfish does **not** auto-compare against your previous run — you name the `before` file yourself (see [Choosing what to compare](#choosing-what-to-compare-against) below).
2. **Method Comparisons**: Compare multiple methods within a single test run — automatic for every `[SailfishMethod]` on a `[Sailfish]` class.

When a comparison runs, SailDiff produces various measurements describing the differences between runs or methods. Results are presented via multiple output formats:

- **Test Output Window**: Real-time results during test execution
- **Consolidated Markdown**: Session-based markdown files with comprehensive comparison data
- **Consolidated CSV**: Session-based CSV files with structured comparison data for analysis

### Method Comparisons

For real-time method comparisons within a single test run, see the [Method Comparisons](/docs/1/method-comparisons) documentation. Comparison is the default behavior for any `[Sailfish]` class — every `[SailfishMethod]` joins an implicit class-wide comparison group. Pick a baseline with `IsBaseline = true` for an N−1 table; opt a class out entirely with `DisableComparison = true`; or set `ComparisonGroup = "..."` for the advanced multi-group case.

Method comparisons generate:
- **N×N or N−1 layouts**: every pair compared (no baseline) or each contender against the chosen baseline
- **Statistical significance testing**: BH-FDR–adjusted q-values and 95% confidence intervals on the ratio
- **Improved / Slower / Similar labels** at α = 0.05
- **Consolidated outputs**: both markdown and CSV formats with `[WriteToMarkdown]` and `[WriteToCsv]`

## Enabling / Configuring SailDiff

If using Sailfish as a test project, you can create a `.sailfish.json` file in the root of your test project (next to your `.csproj` file). This file can hold various configuration settings. If any compatible setting is omitted, a sensible default will be used.

**Note:** Method comparisons run automatically. A **historical** (run-vs-run) comparison happens only when you name a `before` tracking file via `SailDiffSettings.ProvidedBeforeTrackingFiles` (documented below). Without it, each run is simply recorded to a tracking file and no run-vs-run comparison is produced — Sailfish never silently reaches back and compares against your previous run.

**Example `.sailfish.json`**

```json
{
  "SailfishSettings": {
    "DisableOverheadEstimation": false,
    "NumWarmupIterationsOverride": 1,
    "SampleSizeOverride": 30
  },
  "SailDiffSettings": {
    "TestType": "WilcoxonRankSumTest",
    "Alpha": 0.05,
    "Disabled": false
  },
  "ScaleFishSettings": {},
  "GlobalSettings": {
    "UseOutlierDetection": true,
    "ResultsDirectory": "SailfishIDETestOutput",
    "DisableEverything": false,
    "Round": 5
  }
}
```

### SailDiffSettings

**TestType**

Description: Specifies an enum type for a statistical test. One of:

  - WilcoxonRankSumTest (**Default**) — Mann-Whitney U test. The correct non-parametric test for two **independent** samples — i.e., the typical SailDiff scenario of two separate benchmark runs. Robust to the positive skew common in timing data.
  - Test — Welch's two-sample t-test (no equal-variance assumption). Use when you want a CI on the mean difference and your samples are reasonably large or log-distributed.
  - KolmogorovSmirnovTest — Compares full empirical distributions. Less powerful than rank-sum for pure location shifts; use when you suspect distributional shape changes (bimodal latency, regime shifts) rather than a simple "is run B faster?".
  - TwoSampleWilcoxonSignedRankTest — **Paired samples only.** Each before[i] must be paired with after[i] by experimental design. Independent benchmark iterations are not paired; using this on unpaired data produces invalid p-values. Prefer `WilcoxonRankSumTest` for almost all SailDiff use cases.

Note: the JSON value must match the enum member exactly — use `"Test"` (not `"TTest"`) when selecting the t‑test.

**Alpha**

Description: Significance threshold (Type I error rate). The 95% confidence intervals reported alongside each result correspond to `1 − Alpha`. When a run contains multiple comparisons, Benjamini-Hochberg FDR control is applied across the family and the **headline verdict is gated on the adjusted q-value** — a row only reads Regressed/Improved when it survives multiplicity correction, so the verdict and the printed q-value always agree.

Default: `0.05` (matches conventional statistical practice; previous default of `0.001` made detection effectively impossible at typical Sailfish sample sizes). For release-gate runs use the `Tight` preset (`0.01`); for noisy CI hosts use `Relaxed` (`0.10`).

**Disabled**

Description: Disable SailDiff

Default: false

**EquivalenceMarginPercent**

Description: Opt-in TOST (two one-sided tests) equivalence margin, in percent. When set (e.g. `5`), every comparison additionally tests whether the true before/after ratio is **demonstrably inside ±5%**, on the log scale. This splits the weakest verdict — NOT SIGNIFICANT — into two honest statements: *equivalent within the margin* (the run proved similarity) or *inconclusive at the margin* (the run lacked the statistical power to tell; compare the reported MDE against your margin and increase `SampleSize` if needed). Each one-sided test runs at `Alpha`, which keeps the overall error rate ≤ Alpha.

Default: unset (equivalence testing off)

```json
"SailDiffSettings": {
  "TestType": "WilcoxonRankSumTest",
  "Alpha": 0.05,
  "EquivalenceMarginPercent": 5
}
```

**ProvidedBeforeTrackingFiles**

Description: The explicit `before` tracking file(s) to compare this run against. This is how you opt into a **historical** (run-vs-run) comparison from the IDE — Sailfish does not auto-select your previous run. Paths may be absolute or relative to the working directory. When set, each named file is used as a `before` dataset and the current run becomes `after`; when omitted, no run-vs-run comparison is produced.

Default: unset (no historical comparison)

```json
"SailDiffSettings": {
  "ProvidedBeforeTrackingFiles": [
    "SailfishIDETestOutput/sailfish_tracking_output/PerformanceTracking_20260101_120000.json.tracking"
  ]
}
```


#### Example IDE Output

```
Statistical Test
----------------
Test Used:       WilcoxonRankSumTest
PVal Threshold:  0.05
PValue:          0.0528963431
Change:          No Change  (reason: 0.0528963431 > 0.05)

|             | Before (ms) | After (ms) |
| ---         | ---         | ---        |
| Mean        |     61.7671 |    55.0063 |
| Median      |     62.3821 |    56.1542 |
| Sample Size |          30 |         30 |
```

#### Markdown

| Display Name   | MeanBefore (N=7) | MeanAfter (N=7) | MedianBefore | MedianAfter | PValue  | Change Description |
| -------------- | ---------------- | --------------- | ------------ | ----------- | ------- | ------------------ |
| Example.Test() | 190.78 ms        | 191.35 ms       | 187.689 ms   | 186.9367 ms | 0.89023 | No Change          |

The Mean and median are both presented alongside a PValue and Change description. The PValue is returned from the statistical test and compared to a user-set threshold to determine the change description.

### Library

You may use the `RunSettingsBuilder` to configure SailDiff before running.

```csharp
var sailDiffSettings = new SailDiffSettings(
    alpha: 0.05,
    round: 3,
    useOutlierDetection: true,
    testType: TestType.WilcoxonRankSumTest,
    maxDegreeOfParallelism: 4,
    disableOrdering: false);

var runSettings = RunSettingsBuilder
    .CreateBuilder()
    .WithSailDiff(sailDiffSettings)
    .Build();
```

## Choosing what to compare against

A historical comparison runs **only** when you explicitly tell SailDiff which `before` tracking file to use. Sailfish writes a tracking file on every run, but it never auto-selects a previous run to compare against. There are four ways to provide the `before` file:

**1. Builder (console / library)** — pass the file with `WithProvidedBeforeTrackingFile` (or `WithProvidedBeforeTrackingFiles`):

```csharp
var runSettings = RunSettingsBuilder
    .CreateBuilder()
    .WithSailDiff()
    .WithProvidedBeforeTrackingFile("path/to/PerformanceTracking_….json.tracking")
    .Build();
```

**2. Compare against your previous run** — resolve the most recent tracking file explicitly with the `TrackingFiles` helper, then pass it in. This is the deliberate, one-line replacement for the old automatic behaviour:

```csharp
// using Sailfish.Analysis.SailDiff;
var trackingDir = Path.Combine(outputDir, TrackingFiles.DefaultTrackingDirectoryName);
var previousRun = TrackingFiles.MostRecentIn(trackingDir); // null on the first ever run

var builder = RunSettingsBuilder.CreateBuilder().WithSailDiff().WithLocalOutputDirectory(outputDir);
if (previousRun is not null) builder = builder.WithProvidedBeforeTrackingFile(previousRun);
var runSettings = builder.Build();
```

**3. `.sailfish.json` (IDE / test project)** — set `SailDiffSettings.ProvidedBeforeTrackingFiles` (see above).

**4. Custom handler** — supply the inputs yourself by overriding one of the mediator request handlers below (last registration wins, so your handler replaces the default).

The flow of the analysis is

1. Program Execution
1. `TestCaseCompletedNotification`
1. `TestRunCompletedNotification`
1. `BeforeAndAfterFileLocationRequest`
1. `ReadInBeforeAndAfterDataRequest`
1. Saildiff

This flow shows that there are two points at which you can manipulate the data inputs:

- `IRequestHandler<BeforeAndAfterFileLocationRequest, BeforeAndAfterFileLocationResponse>`
- `IRequestHandler<ReadInBeforeAndAfterDataRequest, ReadInBeforeAndAfterDataResponse>`


### Runtime API (in-memory TestData)

In addition to file-based analysis, SailDiff can analyze in-memory `TestData` objects. This is ideal for test adapters and pipelines that already hold results in memory and want to avoid file I/O.

```csharp
// using Sailfish.Analysis;
// var sailDiff = serviceProvider.GetRequiredService<ISailDiff>();
// beforeData/afterData are TestData instances (IDs + PerformanceRunResult list)
sailDiff.Analyze(beforeData, afterData, new SailDiffSettings());
```

Notes:
- Bypasses file I/O; you provide both the test IDs and the `PerformanceRunResult` sequences.
- The file-based flow remains fully supported. For custom file locations or aggregated inputs, use the mediator hooks shown below.

### Reading Tracking Data from a Custom Location

```csharp
// using Sailfish.Analysis.SailDiff; // for the public TrackingFiles helper
internal class SailfishBeforeAndAfterFileLocationHandler
    : IRequestHandler<BeforeAndAfterFileLocationRequest, BeforeAndAfterFileLocationResponse>
{
    private readonly IRunSettings runSettings;

    public SailfishBeforeAndAfterFileLocationHandler(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }

    public Task<BeforeAndAfterFileLocationResponse> Handle(
        BeforeAndAfterFileLocationRequest request,
        CancellationToken cancellationToken)
    {
        // Provide the 'before' (and 'after') datasets however you like — read from a:
        // - database
        // - cloud storage container
        // - cloud log processing tool
        // - network drive
        // - local directory
        //
        // TrackingFiles.AllIn(...) lists local tracking files newest-first. As one possible policy,
        // this handler compares the two most recent runs — but the choice is entirely yours.
        var files = TrackingFiles.AllIn(runSettings.GetRunSettingsTrackingDirectoryPath());

        var before = files.Skip(1).Take(1); // the previous run
        var after = files.Take(1);           // the current run

        return Task.FromResult(new BeforeAndAfterFileLocationResponse(before, after));
    }
}
```

### Reading Tracking Data that you wish to aggregate prior to testing

```csharp
internal class SailfishReadInBeforeAndAfterDataHandler
    : IRequestHandler<ReadInBeforeAndAfterDataRequest, ReadInBeforeAndAfterDataResponse>
{
    public async Task<ReadInBeforeAndAfterDataResponse> Handle(
        ReadInBeforeAndAfterDataRequest request,
        CancellationToken cancellationToken)
    {
        // When you return the data, you are also required to
        // provide an IEnumerable<string> that represents the files that were used.
        return new ReadInBeforeAndAfterDataResponse(
            new TestData(dataSourcesBefore, beforeData),
            new TestData(dataSourcesAfter, afterData));
    }
}
```

If you inspect the `TestData` source code, you will find that it takes an IEnumerable of test Ids, which are intended for you to keep track of which processed files were used in the statistical test.

SailDiff will automatically aggregate data when multiple files are provided.

## Which SailDiff Test should I use?

The default — `WilcoxonRankSumTest` (Mann-Whitney U) — is the right choice for almost every Sailfish use case. It compares two **independent** samples (separate benchmark runs), doesn't assume normality, and is robust to the positive skew typical of timing data.

Use a non-default test only when one of the following applies:

- **`Test` (Welch's t-test)** — when you specifically need a CI on the *mean difference* (rather than a stochastic-dominance statement), and either your N is large enough for the CLT to apply (~30+ per side) or your timings are already log-transformed.
- **`KolmogorovSmirnovTest`** — when you suspect the *shape* of the latency distribution has changed (bimodal latency from a new code path, regime shifts, tail blow-ups), not just its location. KS is less powerful than rank-sum for pure shifts, so don't use it as a general default.
- **`TwoSampleWilcoxonSignedRankTest`** — **only** when your data is genuinely paired by experimental design (same input, same iteration index in a deterministic harness, or repeated measures on the same subject) and you control the pairing. Independent benchmark iterations are not paired; using signed-rank on unpaired data produces invalid p-values.
