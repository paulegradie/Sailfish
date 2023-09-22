---
title: SailDiff
---

## Introduction

**SailDiff** is a tool for running automated **before & after** statistical testing on Sailfish tracking data.

When enabled, tracking data will be used for comparison to the current run and will produce various measurements describing the difference between two runs for each available test case. Depending on how you run Sailfish, SailDiff will presents its results either via StdOut, a test output window, or via an output file.

## Enabling / Configuring SailDiff

### Test Project / IDE

If using Sailfish as a test project, you can create a `.sailfish.json` file in the root of your test project (next to your `.csproj` file). This file can hold various configuration settings. If any compatible setting is omitted, a sensible default will be used.

**Example `.sailfish.json`**

```json
{
  "SailDiffSettings": {
    "TestType": "TTest",
    "Alpha": 0.005,
    "Disabled": false
  },
  "ScaleFishSettings": {},
  "Round": 5,
  "UseOutlierDetection": true,
  "ResultsDirectory": "SailfishIDETestOutput",
  "DisableOverheadEstimation": false,
  "DisableEverything": false
}
```

**Global Settings**

- **Round** - Number of digits to round presented results. **Default: 5**
- **UseOutlierDetection** - Remove outliers (includes test analysis). **Default: true**
- **ResultsDirectory** - Specify a test result directory. **Default: SailfishIDETestOutput**
- **DisableOverheadEstimation** - Disable overhead estimation when iterating test cases (better speed, worse accuracy. **Default: false**
- **DisableEverything** - Disable all analysis features. **Default: false**

**SailDiffSettings**

- **TestType** - Specify a statistical test. **Default: TTest**

  Note: Specifies an enum type. One of:

  - TwoSampleWilcoxonSignedRankTest
  - WilcoxonRankSumTest
  - TTest
  - KolmogorovSmirnovTest

- **Alpha** - Threshold for significance detection. (Aka 'PValue threshold'). **Default: 0.005**
- **Disabled** - Disable SailDiff. **Default: false**

**ScaleFishSettings**

- None

#### Example IDE Output

```
Statistical Test
----------------
Test Used:       TTest
PVal Threshold:  0.005
PValue:          0.0528963431
Change:          No Change  (reason: 0.0528963431 > 0.005)

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
var settings = new SailfDiffSettings(
    alpha: 0.001,
    round = 3,
    useOutlierDetection = false,
    testType = TestType.TTest,
    maxDegreeOfParallelism = 4,
    disableOrdering = false);

var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithSailDiff(settings)
    .Build();
```

## Customizing the SailDiff inputs

By default, Sailfish will look for the most recent file in the default tracking directory when you execute a test run via a console app.

The flow of the analysis is

1. Program Execution
1. WriteDataHandler
1. `IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>`
1. `IRequestHandler<ReadInBeforeAndAfterDataCommand, ReadInBeforeAndAfterDataResponse>`
1. Saildiff / Scalefish

This flow shows that there are two points at which you can minipulate the data inputs:

- IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
- IRequestHandler<ReadInBeforeAndAfterDataCommand, ReadInBeforeAndAfterDataResponse>

### Reading Tracking Data from a Custom Location

```csharp
internal class SailfishBeforeAndAfterFileLocationHandler
    : IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
{
    private readonly ITrackingFileFinder trackingFileFinder;

    public SailfishBeforeAndAfterFileLocationHandler(
        ITrackingFileFinder trackingFileFinder)
    {
        this.trackingFileFinder = trackingFileFinder;
    }

    public Task<BeforeAndAfterFileLocationResponse> Handle(
        BeforeAndAfterFileLocationCommand request,
        CancellationToken cancellationToken)
    {
        var trackingFiles = trackingFileFinder.GetBeforeAndAfterTrackingFiles(
            request.DefaultDirectory,
            request.BeforeTarget,
            request.Tags);
        // Consider reading data from a:
        // - database
        // - cloud storage container
        // - cloud log processing tool
        // - network drive
        // - local directory
        return Task.FromResult(new BeforeAndAfterFileLocationResponse(
            new List<string>() { trackingFiles.BeforeFilePath }.Where(x => !string.IsNullOrEmpty(x)),
            new List<string>() { trackingFiles.AfterFilePath }.Where(x => !string.IsNullOrEmpty(x))));
    }
}
```

### Reading Tracking Data that you wish to aggregate prior to testing

```csharp
internal class SailfishReadInBeforeAndAfterDataHandler
: IRequestHandler<ReadInBeforeAndAfterDataCommand, ReadInBeforeAndAfterDataResponse>
{
    public async Task<ReadInBeforeAndAfterDataResponse> Handle(
        ReadInBeforeAndAfterDataCommand request,
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
