---
title: ScaleFish
---

## Introduction

**Scalefish** is a **machine learning** tool used to perform regression analysis.

When enabled, scalefish will discover test cases with scalefish-enabled variables and use machine learning to fit result data against various parameterized functions representing general complexity curves (e.g. linear, nlogn, etc).

This outputs a model file and a results file once your run is complete. The model file can be used to make predictions.

**NOTE**: The more data you collect, the more accurate these measurements will be.


## Enabling / Configuring ScaleFish

The first thing you'll need to do when enabling **ScaleFish** is to specify a SailfishVariable or SailfishRangeVariable and set the optional complexity boolean to true.

```csharp
[SailfishRangeVariable(true, start: 5, 4, 6)]
public int N { get; set; }

// or

[SailfishVariable(true, 1, 10, 50, 100, 500, 1000)]
public int N { get; set; }

```

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

There are currently no customizations for the ScaleFishSettings.

### Library

You may use the `RunsettingsBuilder` to configure ScaleFish before running.

```csharp
var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithScaleFish()
    .Build();
```

## Results

**Test Class: ScaleFishExample**

| Variable                  | BestFit | BigO | GoodnessOfFit      | NextBest | NextBigO   | NextBestGoodnessOfFit |
| ------------------------- | ------- | ---- | ------------------ | -------- | ---------- | --------------------- |
| ScaleFishExample.Linear.N | Linear  | O(n) | 0.9994126639733568 | NLogN    | O(nLog(n)) | 0.9942376556590526    |

For each variable, all other variables will be held constant at their smallest scale. For each parameterized function, regression will be performed to fit the model to the data. For each resulting model, a goodness of fit is calculated and best two fitting models are returned. Using this result, you can guadge the general complexity of the logic inside the SailfishMethod.

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

## Making predictions

Sailfish provides basic tools for loading models and making predictions.

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

For a working example, [visit the demo](https://github.com/paulegradie/Sailfish/blob/main/source/ScaleFishDemo/Program.cs).
