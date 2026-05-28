---
title: SailDiff
---

## Introduction

**SailDiff** is a tool for running automated statistical testing on Sailfish performance data. It provides powerful comparison capabilities to help you understand performance changes and differences.

SailDiff operates in two main modes:

1. **Historical Comparisons**: Compare current test runs against previously saved tracking data
2. **Method Comparisons**: Compare multiple methods within a single test run using the `[SailfishComparison]` attribute

When enabled, SailDiff will produce various measurements describing the differences between test runs or methods. Results are presented via multiple output formats:

- **Test Output Window**: Real-time results during test execution
- **Consolidated Markdown**: Session-based markdown files with comprehensive comparison data
- **Consolidated CSV**: Session-based CSV files with structured comparison data for analysis

### Method Comparisons

For real-time method comparisons within a single test run, see the [Method Comparisons](/docs/1/method-comparisons) documentation. This feature allows you to compare multiple algorithms or implementations automatically using the `[SailfishComparison("GroupName")]` attribute.

Method comparisons generate:
- **N×N comparison matrices**: Every method compared against every other method in the same group
- **Statistical significance testing**: P-values and confidence intervals
- **Performance ratios**: Clear "X times faster/slower" descriptions
- **Consolidated outputs**: Both markdown and CSV formats available with `[WriteToMarkdown]` and `[WriteToCsv]` attributes

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
    "TestType": "Test",
    "Alpha": 0.001,
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

  - TwoSampleWilcoxonSignedRankTest (**Default**)
  - WilcoxonRankSumTest
  - KolmogorovSmirnovTest
  - Test (Student's t‑test)

Note: the JSON value must match the enum member exactly — use `"Test"` (not `"TTest"`) when selecting the t‑test.

**Alpha**

Description: Threshold for significance detection. (Aka 'PValue threshold').

Default: 0.001 (library) / 0.0001 (test adapter)

**Disabled**

Description: Disable SailDiff

Default: false


#### Example IDE Output

```
Statistical Test
----------------
Test Used:       Test
PVal Threshold:  0.001
PValue:          0.0528963431
Change:          No Change  (reason: 0.0528963431 > 0.001)

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
    alpha: 0.001,
    round: 3,
    useOutlierDetection: true,
    testType: TestType.Test,
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

When customizing the TestSettings **TestType** (either via .sailfish.json or RunSettingsBuilder), you have four options to choose from.

You can follow this rule of thumb when choosing:

```python
if (your test makes requests over a network):
    One of:
    - TwoSampleWilcoxonSignedRankTest
    - WilcoxonRankSumTest
    - KolmogorovSmirnovTest
else:
    - Test (Student's t‑test)
```
