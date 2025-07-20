---
title: SailDiff
---

SailDiff is Sailfish's powerful regression detection system that automatically compares performance data between different runs to identify performance changes.

{% info-callout title="What is SailDiff?" %}
SailDiff performs automated **before & after** statistical testing on Sailfish tracking data, helping you catch performance regressions before they reach production.
{% /info-callout %}

## 🔍 How SailDiff Works

{% success-callout title="Automated Regression Detection" %}
SailDiff follows a systematic approach to identify performance changes with statistical rigor.
{% /success-callout %}

**1. Data Collection** - SailDiff uses tracking data from previous test runs as a baseline for comparison.

**2. Statistical Analysis** - When you run tests, SailDiff compares current results against historical data using appropriate statistical tests.

**3. Regression Detection** - SailDiff automatically identifies significant performance changes and categorizes them as improvements, regressions, or no change.

When enabled, tracking data will be used for comparison to the current run and will produce various measurements describing the difference between two runs for each available test case. Depending on how you run Sailfish, SailDiff presents its results either via StdOut, a test output window, or via an output file.

{% feature-grid columns=3 %}
{% feature-card title="Automated Detection" description="No manual comparison needed - SailDiff automatically identifies performance changes." /%}

{% feature-card title="Statistical Rigor" description="Uses proven statistical methods to ensure changes are significant, not just noise." /%}

{% feature-card title="Multiple Output Formats" description="Results available in console, test output, or files depending on your workflow." /%}
{% /feature-grid %}

## ⚙️ Enabling SailDiff

{% tip-callout title="Easy Configuration" %}
SailDiff can be enabled through configuration files or programmatically, depending on how you're using Sailfish.
{% /tip-callout %}

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
    "TestType": "TTest",
    "Alpha": 0.005,
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

### 🔧 SailDiffSettings

{% feature-grid columns=3 %}
{% feature-card title="TestType" description="Statistical test to use for comparison. Default: TTest" /%}

{% feature-card title="Alpha" description="P-value threshold for significance detection. Default: 0.005" /%}

{% feature-card title="Disabled" description="Enable or disable SailDiff analysis. Default: false" /%}
{% /feature-grid %}

**Available Test Types:**
- **TTest** (**Default**) - Best for most scenarios
- **TwoSampleWilcoxonSignedRankTest** - Non-parametric alternative
- **WilcoxonRankSumTest** - For independent samples
- **KolmogorovSmirnovTest** - Distribution comparison

{% tip-callout title="Test Selection Guide" %}
Use **TTest** for most scenarios. Consider non-parametric tests (Wilcoxon, Kolmogorov-Smirnov) when dealing with network requests or non-normal distributions.
{% /tip-callout %}


## 📊 Output Examples

### 🖥️ IDE Output

{% code-callout title="Console Results" %}
SailDiff provides detailed statistical analysis directly in your IDE or console output.
{% /code-callout %}

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

### 📝 Markdown Output

{% success-callout title="Report Ready" %}
Markdown output is perfect for including regression analysis in documentation and reports.
{% /success-callout %}

| Display Name   | MeanBefore (N=7) | MeanAfter (N=7) | MedianBefore | MedianAfter | PValue  | Change Description |
| -------------- | ---------------- | --------------- | ------------ | ----------- | ------- | ------------------ |
| Example.Test() | 190.78 ms        | 191.35 ms       | 187.689 ms   | 186.9367 ms | 0.89023 | No Change          |

{% info-callout title="Statistical Interpretation" %}
The Mean and Median are presented alongside a P-Value and Change description. The P-Value is returned from the statistical test and compared to your threshold to determine the change description.
{% /info-callout %}

### 📚 Library Configuration

{% code-callout title="Programmatic Setup" %}
You may use the `RunSettingsBuilder` to configure SailDiff programmatically when using Sailfish as a library.
{% /code-callout %}

```csharp
var settings = new SailfDiffSettings(
    alpha: 0.001,
    round: 3,
    useOutlierDetection: true,
    testType: TestType.TTest,
    maxDegreeOfParallelism: 4,
    disableOrdering: false);

var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithSailDiff(settings)
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

This flow shows that there are two points at which you can minipulate the data inputs:

- IRequestHandler<BeforeAndAfterFileLocationRequest, BeforeAndAfterFileLocationResponse>
- IRequestHandler<ReadInBeforeAndAfterDataCommand, ReadInBeforeAndAfterDataResponse>

### Reading Tracking Data from a Custom Location

```csharp
internal class SailfishBeforeAndAfterFileLocationHandler
    : IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
{
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;

    public SailfishBeforeAndAfterFileLocationHandler(
        IRunSettings runSettings,
        ITrackingFileDirectoryReader trackingFileDirectoryReader)
    {
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
        this.runSettings = runSettings
    }

    public Task<BeforeAndAfterFileLocationResponse> Handle(
        BeforeAndAfterFileLocationCommand request,
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

## 🤔 Which SailDiff Test Should I Use?

{% tip-callout title="Test Selection Guide" %}
When customizing the TestSettings **TestType** (either via .sailfish.json or RunSettingsBuilder), follow this simple rule of thumb.
{% /tip-callout %}

```python
if (your test makes requests over a network):
    # Use non-parametric tests for network variability
    One of:
        - TwoSampleWilcoxonSignedRankTest
        - WilcoxonRankSumTest
        - KolmogorovSmirnovTest
else:
    # Use parametric test for local operations
    - TTest
```

{% feature-grid columns=2 %}
{% feature-card title="Network Operations" description="Use non-parametric tests (Wilcoxon, Kolmogorov-Smirnov) for HTTP requests, database calls, or any network-dependent operations." /%}

{% feature-card title="Local Operations" description="Use TTest for in-memory operations, algorithms, and other deterministic local computations." /%}
{% /feature-grid %}

{% note-callout title="Advanced Usage" %}
For advanced SailDiff customization including custom data sources and aggregation, see the extensibility examples in the full documentation. You can also explore [ScaleFish](/docs/2/scalefish) for algorithmic complexity analysis.
{% /note-callout %}
