---
title: Sailfish
---

Sailfish is the core performance test runner that executes your performance tests and provides detailed statistical analysis. It offers flexible deployment options and comprehensive output formats.

{% info-callout title="Core Test Runner" %}
Sailfish is the engine that executes your performance tests, applies statistical analysis, and generates detailed reports. It can be used as a test project for IDE integration or as a library for custom workflows.
{% /info-callout %}

## 🚀 Deployment Options

{% feature-grid columns=2 %}
{% feature-card title="Test Project" description="Run Sailfish tests directly from your IDE like NUnit or xUnit tests with full debugging support." /%}

{% feature-card title="Library Integration" description="Embed Sailfish into applications for custom workflows and production monitoring." /%}
{% /feature-grid %}

## 🧪 Test Project Approach

{% success-callout title="IDE Integration" %}
When used as a test project, you can run Sailfish tests directly from the IDE - similar to how you might run an NUnit or xUnit test.
{% /success-callout %}

### ⚙️ Configuration with .sailfish.json

{% tip-callout title="Flexible Configuration" %}
Create a `.sailfish.json` file in the root of your test project (next to your `.csproj` file) to customize execution settings. If any setting is omitted, sensible defaults will be used.
{% /tip-callout %}

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

### 🔧 IDE-Specific Setup

{% warning-callout title="JetBrains Rider Setup" %}
If you are using JetBrains Rider, you will need to [enable VS Test Adapter Support](https://www.jetbrains.com/help/rider/Reference__Options__Tools__Unit_Testing__VSTest.html).
{% /warning-callout %}

**Important for Rider users:** Tests from VSTest adapters are only discovered after test projects are built, unlike frameworks natively supported by JetBrains Rider.


## 📊 Test Output

{% code-callout title="Rich Statistical Analysis" %}
Sailfish provides comprehensive statistical analysis with descriptive statistics, outlier detection, and distribution analysis.
{% /code-callout %}

A typical test run produces the following output in the test output window:

```
ReadmeExample.TestMethod

Descriptive Statistics
----------------------
| Stat   |  Time (ms) |
| ---    | ---        |
| Mean   |     62.411 |
| Median |     62.986 |
| StdDev |     1.4544 |
| Min    |    59.8693 |
| Max    |    63.4148 |

Outliers Removed (0)
--------------------

Distribution (ms)
-----------------
59.8693, 62.5765, 62.986, 63.4148, 63.2082
```

{% info-callout title="Output Locations" %}
These are the basic descriptive statistics describing your Sailfish test run. Persisted outputs (such as markdown or CSV files) will be found in the output directory in the calling assembly's **/bin** folder.
{% /info-callout %}

## 📚 Library Integration

{% success-callout title="Programmatic Control" %}
Running Sailfish as a library gives you full programmatic control over test execution and configuration.
{% /success-callout %}

```csharp
using Sailfish;

var settings = RunSettingsBuilder.CreateBuilder().Build();
await SailfishRunner.Run(settings);
```

## 📄 Output Formats

### 📝 Markdown Output

{% tip-callout title="Documentation Ready" %}
Markdown output is perfect for including performance results in documentation, README files, or technical reports.
{% /tip-callout %}

| Display Name | Mean | Median | StdDev (N=5) | Variance |
| ------------ | ---- | ------ | ------------ | -------- |
| Example.Minimal() | 62.41 ms | 62.99 ms | 1.45 ms | 2.12 |

### 📊 CSV Output

{% code-callout title="Data Analysis Ready" %}
CSV output provides raw data that can be imported into Excel, R, Python, or other analysis tools.
{% /code-callout %}

```csv
DisplayName,Median,Mean,StdDev,Variance,LowerOutliers,UpperOutliers,TotalNumOutliers,SampleSize,RawExecutionResults
MinimalTestExample.Minimal(),62.3577,59.79556,5.118877113488853,26.202902902999977,,,0,5,"61.0641,62.370000000000005,62.4878,50.6982,62.3577"
```

{% note-callout title="Next Steps" %}
Explore [SailDiff](/docs/2/saildiff) for regression testing capabilities, or learn about [ScaleFish](/docs/2/scalefish) for algorithmic complexity analysis.
{% /note-callout %}
