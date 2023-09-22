---
title: ScaleFish
---

## Introduction

**Scalefish** is a **machine learning** tool used to perform regression analysis.

When enabled, scalefish will discover test cases with scalefish-enabled variables and use them to fit result data against various parameterized functions representing general complexity curves.

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

### Example output

Test Class: AllTheFeatures

| Variable                          | BestFit   | BigO  | GoodnessOfFit     | NextBest    | NextBigO | NextBestGoodnessOfFit |
| --------------------------------- | --------- | ----- | ----------------- | ----------- | -------- | --------------------- |
| AllTheFeatures.SlowerMethod.Delay | Factorial | O(n!) | 0.999607514119086 | Exponential | O(2^n)   | 0.9568836099884173    |

| Variable                          | BestFit | BigO       | GoodnessOfFit     | NextBest | NextBigO | NextBestGoodnessOfFit |
| --------------------------------- | ------- | ---------- | ----------------- | -------- | -------- | --------------------- |
| AllTheFeatures.FasterMethod.Delay | SqrtN   | O(sqrt(n)) | 0.736267193815595 | Linear   | O(n)     | 0.7282730964468905    |
