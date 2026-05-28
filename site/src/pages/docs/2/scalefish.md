---
title: ScaleFish
---

## Introduction

**Scalefish** is a **regression analysis** tool that infers the algorithmic complexity of your test method as the input scale grows.

When enabled, scalefish will discover test cases with scalefish-enabled variables and fit the timing data against a catalog of complexity curves (Linear, NLogN, Quadratic, Cubic, SqrtN, LogLinear, Exponential, Factorial). It ranks the candidates using the **corrected Akaike Information Criterion (AICc)** and reports a confidence-aware classification with a continuous-exponent diagnostic.

This outputs a model file and a results file once your run is complete. The model file can be used to make predictions.

**NOTE**: The more data you collect, the more accurate these measurements will be — but with the recommended geometric (log-spaced) layout below, even 4–6 well-chosen X values can robustly distinguish complexity classes.


## Enabling / Configuring ScaleFish

The first thing you'll need to do when enabling **ScaleFish** is to specify a SailfishVariable or SailfishRangeVariable and set the optional complexity boolean to true.

```csharp
// Geometric (log-spaced) — RECOMMENDED for complexity probes.
// Produces N ∈ {8, 16, 32, 64, 128, 256}: equally spaced in log-x for maximum discrimination per data point.
[SailfishRangeVariable(scaleFish: true, start: 8, end: 256, count: 6, spacing: RangeSpacing.Geometric)]
public int N { get; set; }

// Linear-spaced range (the original constructor).
[SailfishRangeVariable(scaleFish: true, start: 5, count: 6, step: 4)]
public int N { get; set; }

// Explicit values.
[SailfishVariable(true, 1, 10, 50, 100, 500, 1000)]
public int N { get; set; }
```

### Why log-spacing?

Distinguishing Linear from NLogN, or Quadratic from Cubic, is fundamentally a **log-x** problem: across a 10× linear range the curves are nearly parallel, but across a 10× geometric range they fan out dramatically. Geometric spacing gives substantially more discrimination per data point — which means **fewer X values produce a more confident answer**.

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

| Variable                  | BestFit | BigO | GoodnessOfFit | NextBest | NextBigO   | NextBestGoodnessOfFit | DeltaAICc | AkaikeWeight | Distinguishable | ContinuousExponent |
| ------------------------- | ------- | ---- | ------------- | -------- | ---------- | --------------------- | --------- | ------------ | --------------- | ------------------ |
| ScaleFishExample.Linear.N | Linear  | O(n) | 0.99          | NLogN    | O(nLog(n)) | 0.99                  | 18.4      | 0.999        | yes             | b=1.03, c=0.02     |

For each variable, all other variables are held constant at their smallest scale. For each candidate family the estimator performs a **linear-in-parameters fit** (using `y = scale · basis(x) + bias`, with variance-weighting when replicate uncertainty is available) and computes:

- **GoodnessOfFit / NextBestGoodnessOfFit** — R² of the best and next-best discrete families.
- **DeltaAICc** — Δ between the next-best and best AICc. ≥ 2 means the best model is statistically separable from the runner-up; smaller values mean the data don't clearly prefer one over the other.
- **AkaikeWeight** — the share of total Akaike support that goes to the best model (between 0 and 1). 1.0 = effectively single-winner.
- **Distinguishable** — `yes` when DeltaAICc ≥ 2, `no` when the top two candidates are too close to call.
- **ContinuousExponent** — `b` and `c` from a continuous `y = a · x^b · (log x)^c + d` fit. Useful when reality is between two textbook curves (e.g. `b = 1.4, c = 0`: between Linear and Quadratic).

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

## How ScaleFish picks the best fit

For each candidate family, ScaleFish:

1. Computes the **linearizing basis** `basis(x) = f(0, 1, x)` (e.g. `x` for Linear, `x · ln(x)` for NLogN, `x²` for Quadratic). For Exponential and Factorial, an automatic **log-space fallback** kicks in when the raw basis would overflow.
2. Solves the **linear-in-parameters least squares** problem `y_i ≈ scale · basis(x_i) + bias`. When the measurements carry replicate uncertainty (Sailfish's `SampleSize × StdDev`), this becomes **weighted least squares** with weights `1 / SE²` — points with tighter measurements have more pull.
3. Scores the fit with **AICc** (small-sample-corrected Akaike). The candidate with the lowest AICc wins.
4. Reports the **Akaike weight** of the winner and a **distinguishability flag** (`yes` when ΔAICc ≥ 2).
5. If raw replicate samples are present, runs a deterministic **bootstrap** (default 200 iterations, seeded from the data) to produce parameter CIs and a **selection agreement** metric — the fraction of bootstrap iterations that re-selected the same best-family.

This pipeline is robust to noise, well-conditioned even for poorly-scaled families (factorial fits in log-Gamma space), and uses the replicate variance information Sailfish already collects.
