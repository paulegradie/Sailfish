---
title: ScaleFish
---

ScaleFish is Sailfish's machine learning-powered complexity analysis tool that automatically determines the algorithmic complexity (Big O) of your code.

{% success-callout title="Machine Learning Analysis" %}
**ScaleFish** uses machine learning to perform regression analysis, fitting your performance data against various complexity curves (linear, O(n log n), quadratic, etc.) to determine your algorithm's Big O complexity.
{% /success-callout %}

When enabled, ScaleFish will discover test cases with ScaleFish-enabled variables and use machine learning to fit result data against various parameterized functions representing general complexity curves.

This outputs a model file and a results file once your run is complete. The model file can be used to make predictions about performance at different scales.

{% warning-callout title="Data Quality Matters" %}
**NOTE**: The more data you collect, the more accurate these measurements will be. Use sufficient sample sizes and variable ranges for reliable complexity analysis.
{% /warning-callout %}


## ⚙️ Enabling ScaleFish

{% tip-callout title="Variable Configuration" %}
The first thing you'll need to do when enabling **ScaleFish** is to specify a SailfishVariable or SailfishRangeVariable and set the optional complexity boolean to `true`.
{% /tip-callout %}

### 🔢 Variable Setup

```csharp
[SailfishRangeVariable(true, start: 5, count: 4, step: 6)]
public int N { get; set; }

// or

[SailfishVariable(true, 1, 10, 50, 100, 500, 1000)]
public int N { get; set; }
```

{% feature-grid columns=2 %}
{% feature-card title="SailfishRangeVariable" description="Generate systematic ranges for complexity analysis with start, count, and step parameters." /%}

{% feature-card title="SailfishVariable" description="Specify discrete values that represent different scales for your algorithm." /%}
{% /feature-grid %}

### 📁 Configuration File

{% code-callout title="JSON Configuration" %}
If using Sailfish as a test project, create a `.sailfish.json` file in the root of your test project (next to your `.csproj` file) to enable ScaleFish.
{% /code-callout %}

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

{% info-callout title="Simple Configuration" %}
There are currently no customizations for the ScaleFishSettings - just include the empty object to enable the feature.
{% /info-callout %}

### 📚 Library Configuration

{% tip-callout title="Programmatic Setup" %}
You may use the `RunSettingsBuilder` to configure ScaleFish when using Sailfish as a library.
{% /tip-callout %}

```csharp
var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithScaleFish()
    .Build();
```

## 📊 Analysis Results

{% success-callout title="Complexity Analysis Output" %}
ScaleFish provides detailed complexity analysis with goodness-of-fit metrics to help you understand your algorithm's performance characteristics.
{% /success-callout %}

**Test Class: ScaleFishExample**

| Variable                  | BestFit | BigO | GoodnessOfFit      | NextBest | NextBigO   | NextBestGoodnessOfFit |
| ------------------------- | ------- | ---- | ------------------ | -------- | ---------- | --------------------- |
| ScaleFishExample.Linear.N | Linear  | O(n) | 0.9994126639733568 | NLogN    | O(nLog(n)) | 0.9942376556590526    |

{% info-callout title="Analysis Process" %}
For each variable, all other variables are held constant at their smallest scale. For each parameterized function, regression is performed to fit the model to the data. The goodness of fit is calculated and the best two fitting models are returned, helping you gauge the general complexity of the logic inside the SailfishMethod.
{% /info-callout %}

## Models

In addition, a model file is produced with content similar to:

```json
[
  {
    "TestClassName": "ScaleFishExample",
    "ScaleFishMethodModels": [
      {
        "TestMethodName": "Linear",
        "ScaleFishPropertyModels": [
          {
            "PropertyName": "ScaleFishExample.Linear.N",
            "ScalefishModel": {
              "ScaleFishModelFunction": {
                "Name": "Linear",
                "OName": "O(n)",
                "Quality": "Good",
                "FunctionDef": "f(x) = {0}x \u002B {1}",
                "FunctionParameters": {
                  "Scale": 12.567931794629985,
                  "Bias": 8.049917490507069
                }
              },
              "GoodnessOfFit": 0.9994126639733568,
              "NextClosestScaleFishModelFunction": {
                "Name": "NLogN",
                "OName": "O(nLog(n))",
                "Quality": "Good",
                "FunctionDef": "f(x) = {0}xLog_e(x) \u002B {1}",
                "FunctionParameters": {
                  "Scale": 2.8717369653838825,
                  "Bias": 76.81277766854257
                }
              },
              "NextClosestGoodnessOfFit": 0.9942376556590526
            }
          }
        ]
      }
    ]
  }
]
```

## 🔮 Making Predictions

{% code-callout title="Predictive Modeling" %}
Sailfish provides tools for loading models and making predictions about performance at different scales.
{% /code-callout %}

```csharp
var model = ModelLoader
  .LoadModelFile("ScalefishModels_#####_####.json")
  .GetScalefishModel(
    nameof(ScaleFishExample),
    nameof(ScaleFishExample.Linear),
    nameof(ScaleFishExample.N));

var result = model.ScaleFishModelFunction.Predict(50_000);
Console.WriteLine(result);
```

### 🎯 Use Cases

{% feature-grid columns=2 %}
{% feature-card title="Capacity Planning" description="Predict performance at different scales to plan infrastructure and resources." /%}

{% feature-card title="Algorithm Selection" description="Compare different algorithms' complexity to choose the best approach for your scale." /%}
{% /feature-grid %}

{% note-callout title="Learn More" %}
For a working example, [visit the demo](https://github.com/paulegradie/Sailfish/blob/main/source/ScaleFishDemo/Program.cs). Ready to explore more? Check out our [Advanced Usage](/docs/3/extensibility) guide or learn about [Example Applications](/docs/3/example-app).
{% /note-callout %}
