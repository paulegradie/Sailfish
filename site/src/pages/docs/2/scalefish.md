---
title: ScaleFish
---

## Introduction

**Scalefish** is a **machine learning** tool used to perform regression analysis.

When enabled, scalefish will discover test cases with scalefish-enabled variables and use machine learning to fit result data against various parameterized functions representing general complexity curves (e.g. linear, nlogn, etc).

This outputs a model file and a results file once your run is complete. The model file can be used to make predictions.

## Enabling / Configuring ScaleFish

The first thing you'll need to do when enabling **ScaleFish** is to specify a SailfishVariable or SailfishRangeVariable and set the optional complexity boolean to true.

```csharp
[SailfishRangeVariable(true, start: 5, 4, 6)]
public int N { get; set; }

// or

[SailfishVariable(true, 1, 10, 50, 100, 500, 1000)]
public int N { get; set; }

```

### Test Project / IDE

If using Sailfish as a test project, you can create a `.sailfish.json` file in the root of your test project (next to your `.csproj` file). This file can hold various configuration settings. If any compatible setting is omitted, a sensible default will be used.

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

There are currently no customizations for the ScaleFishSettings.

There are currently no IDE outputs for ScaleFish.

### Library

You may use the `RunsettingsBuilder` to configure ScaleFish before running.

```csharp
var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithScaleFish()
    .Build();
```
## Results

**Test Class: SailfishFixtureExample**

| Variable              | BestFit      | BigO       | GoodnessOfFit | NextBest           | NextBigO | NextBestGoodnessOfFit |
| --------------------- | ------------ | ---------- | ------------- | ------------------ | -------- | --------------------- |
| Example.Test.Variable | SqrtN (best) | O(sqrt(n)) | 0.81442892    | Linear (next best) | O(n)     | 0.7316056             |

For each variable, all other variables will be held constant at their smallest scale. For each parameterized function, regression will be performed to fit the model to the data. For each resulting model, a goodness of fit is calculated and best two fitting models are returned. Using this result, you can guadge the general complexity of the logic inside the SailfishMethod.

## Models

In addition, a model file is produced with content similar to:

```json
[
  {
    "TestClassName": "Example",
    "ScaleFishMethodModels": [
      {
        "TestMethodName": "Test",
        "ScaleFishPropertyModels": [
          {
            "PropertyName": "Example.Test.Variable",
            "ScalefishModel": {
              "ScaleFishModelFunction": {
                "Name": "SqrtN",
                "OName": "O(sqrt(n))",
                "Quality": "Okay",
                "FunctionDef": "f(x) = {0}sqrt(x) + {1}",
                "FunctionParameters": {
                  "Scale": 1.0749999999999997,
                  "Bias": 1.0750000000000004e-5
                }
              },
              "GoodnessOfFit": 0.8144289259547902,
              "NextClosestScaleFishModelFunction": {
                "Name": "Linear",
                "OName": "O(n)",
                "Quality": "Good",
                "FunctionDef": "f(x) = {0}x + {1}",
                "FunctionParameters": {
                  "Scale": 1.0749999999999997,
                  "Bias": 1.0750000000000004e-5
                }
              },
              "NextClosestGoodnessOfFit": 0.7316056315214764
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
var file = "Path/To/Your/Model/File.json
var model = ModelLoader
  .LoadModelFile(file)
  .GetScalefishModel(
    nameof(Example),
     nameof(ScaleFishExample.Test),
      nameof(ScaleFishExample.Variable));

var result = model.ScaleFishModelFunction.Predict(50_000);
Console.WriteLine(result);

```

For a working example, [visit the demo](https://github.com/paulegradie/Sailfish/blob/main/source/ModelPredictions/Program.cs).
