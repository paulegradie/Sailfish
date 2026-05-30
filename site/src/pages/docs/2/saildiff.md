---
title: SailDiff
---

## Introduction

**SailDiff** is a tool for running automated statistical testing on Sailfish performance data. It provides powerful comparison capabilities to help you understand performance changes and differences.

SailDiff operates in two main modes:

1. **Historical Comparisons**: Compare current test runs against previously saved tracking data
2. **Method Comparisons**: Compare multiple methods within a single test run — automatic for every `[SailfishMethod]` on a `[Sailfish]` class

When enabled, SailDiff will produce various measurements describing the differences between test runs or methods. Results are presented via multiple output formats:

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

If using Sailfish as a test project, you can create a `.sailfish.json` file in the root of your test project (next to your `.csproj` file). This file can hold various configuration settings. When found, SailDiff will be automatically run. If any compatible setting is omitted, a sensible default will be used.

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

Description: Significance threshold (Type I error rate). The 95% confidence intervals reported alongside each result correspond to `1 − Alpha`.

Default: `0.05` (matches conventional statistical practice; previous default of `0.001` made detection effectively impossible at typical Sailfish sample sizes). For release-gate runs use the `Tight` preset (`0.01`); for noisy CI hosts use `Relaxed` (`0.10`).

**Disabled**

Description: Disable SailDiff

Default: false


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

## Customizing the SailDiff inputs

By default, Sailfish will look for the most recent file in the default tracking directory when you execute a test run via a console app.

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
- The file-based flow remains fully supported. For custom file locations or aggregated inputs, use the MediatR hooks shown below.

### Reading Tracking Data from a Custom Location

```csharp
internal class SailfishBeforeAndAfterFileLocationHandler
    : IRequestHandler<BeforeAndAfterFileLocationRequest, BeforeAndAfterFileLocationResponse>
{
    private readonly IRunSettings runSettings;
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;

    public SailfishBeforeAndAfterFileLocationHandler(
        IRunSettings runSettings,
        ITrackingFileDirectoryReader trackingFileDirectoryReader)
    {
        this.runSettings = runSettings;
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
    }

    public Task<BeforeAndAfterFileLocationResponse> Handle(
        BeforeAndAfterFileLocationRequest request,
        CancellationToken cancellationToken)
    {
        var trackingFiles = trackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(
                runSettings.GetRunSettingsTrackingDirectoryPath(),
                ascending: false);

        // Consider reading data from a:
        // - database
        // - cloud storage container
        // - cloud log processing tool
        // - network drive
        // - local directory

        return Task.FromResult(new BeforeAndAfterFileLocationResponse(
            trackingFiles.BeforeFilePaths.Where(x => !string.IsNullOrEmpty(x)),
            trackingFiles.AfterFilePaths.Where(x => !string.IsNullOrEmpty(x))));
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
