---
title: Sailfish
---

Sailfish is the performance test runner. Tests are implemented using Sailfish attributes and timed execution results can be written to StdOut or various file formats.

Different outputs are availabe depending on where you use Sailfish as test project or a class library.

## Test Project

When used as a test project, you can run Sailfish tests directly from the IDE - similar to how you might run an NUnit or xUnit test.

### Test Project / IDE

If using Sailfish as a test project, you can create a `.sailfish.json` file in the root of your test project (next to your `.csproj` file). This file can hold various configuration settings. If any compatible setting is omitted, a sensible default will be used.

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


{% callout title="JetBrains Rider setup" type="warning" %}
If you are using JetBrains Rider, you need to [enable VS Test Adapter Support](https://www.jetbrains.com/help/rider/Reference__Options__Tools__Unit_Testing__VSTest.html).

In contrast to test frameworks natively supported by Rider, tests from VSTest adapters are only discovered after test projects are built.
{% /callout %}


## Outputs

A run will produce the following result in the test output window:

```
ReadmeExample.TestMethod

Descriptive Statistics
----------------------
| Stat   |  Time (ms) |
| ---    | ---        |
| Mean   |     62.411 |
| Median |     62.986 |
| 95% CI ± |     11.0496 |
| 99% CI ± |     14.9900 |
| StdDev |     1.4544 |
| Min    |    59.8693 |
| Max    |    63.4148 |


Outliers Removed (0)
--------------------

Distribution (ms)
-----------------
59.8693, 62.5765, 62.986, 63.4148, 63.2082
```

Plain-English CI: If you repeated the experiment many times, 95% of such intervals would contain the true average runtime. 99% is wider (more conservative) than 95%.

Adaptive precision: CI margins are formatted with adaptive precision (try 4 decimals → if zero, try 6 → then 8 → then show "0").

{% callout title="Terminal encoding" type="note" %}
Depending on your terminal or console encoding, the ± symbol may appear as a replacement character. This is cosmetic and does not affect values.
{% /callout %}

These are the basic descriptive statistics describing your Sailfish test run. Persisted outputs (such as markdown or csv files) will be found the output directory in the calling assembly's **/bin** folder.

## Library

Running Sailfish as a library can be done like so:

```csharp
using Sailfish;

var settings = RunSettingsBuilder.CreateBuilder().Build();
await SailfishRunner.Run(settings);
```

### Markdown


Example

| Display Name                 | Mean                  | Median    | StdDev (N=5)          | Variance          |
| ---------------------------- | --------------------- | --------- | --------------------- | ----------------- |
| Example.Minimal() | 62.410959999999996 ms | 62.986 ms | 1.4544239763562794 ms | 2.115349103000011 |

### CSV

```csv
DisplayName,Median,Mean,StdDev,Variance,LowerOutliers,UpperOutliers,TotalNumOutliers,SampleSize,RawExecutionResults
MinimalTestExample.Minimal(),62.3577,59.79556,5.118877113488853,26.202902902999977,,,0,5,"61.0641,62.370000000000005,62.4878,50.6982,62.3577"
```
