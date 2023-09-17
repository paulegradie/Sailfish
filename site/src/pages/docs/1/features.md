---
title: Features
---

# Sailfish

## IDE

When run in the IDE, Sailfish produces the following result in the test output window:

```
ReadmeExample.TestMethod

Descriptive Statistics
----------------------
| Stat   |  Time (ms) |
| ---    | ---        |
| Mean   |   111.1442 |
| Median |   107.8113 |
| StdDev |     7.4208 |
| Min    |   105.9743 |
| Max    |   119.6471 |


Outliers Removed (0)
--------------------

Adjusted Distribution (ms)
--------------------------
119.6471, 105.9743, 107.8113
```

These are the basic descriptive statistics describing your Sailfish test run. Persisted outputs (such as markdown or csv files) will be found the output directory in the calling assembly's **/bin** folder. Those results will

## Markdown

MinimalTestExample

| Display Name   | Mean         | Median    | StdDev (N=3) | Variance   |
| -------------- | ------------ | --------- | ------------ | ---------- |
| Example.Test() | 60.849633 ms | 62.928 ms | 4.0073198 ms | 16.0586121 |


# SailDiff

Saildiff will attempt to compare previous results with current results. Saildiff will scan the tracking directory for prior test runs when tracking test results and use them to compute a statistical analysis to determine performance changes.

| Display Name   | MeanBefore (N=7) | MeanAfter (N=7) | MedianBefore | MedianAfter | PValue  | Change Description |
| -------------- | ---------------- | --------------- | ------------ | ----------- | ------- | ------------------ |
| Example.Test() | 190.78 ms        | 191.35 ms       | 187.689 ms   | 186.9367 ms | 0.89023 | No Change          |

The Mean and median are both presented alongside a PValue and Change description. The PValue is returned from the statistical test and compared to a user-set threshold to determine the change description.

# ScaleFish

Scalefish will attempt to use machine learning to fit any scalefish enabled variables to one of several classic algorithmic complexity functions (e.g. linear, nlogn, etc).

## Result

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
