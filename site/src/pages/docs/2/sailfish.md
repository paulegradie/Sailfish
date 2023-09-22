---
title: Sailfish
---

Sailfish is the performance test runner. Tests are implemented using Sailfish attributes and timed execution results can be written to StdOut or various file formats.

Different outputs are availabe depending on where you use Sailfish as test project or a class library.

## Test Project

When used as a test project, you can run Sailfish tests directly from the IDE - similar to how you might run an NUnit or xUnit test.

⚠️ If you are using Jetbrains Rider - you will need to [enable VS Test Adapter Support](https://www.jetbrains.com/help/rider/Reference__Options__Tools__Unit_Testing__VSTest.html)

**In contrast to test frameworks natively supported by JetBrains Rider, tests from VSTest adapters are only discovered after test projects are build.**

A run will produce the following result in the test output window:

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

| Display Name      | Mean         | Median     | StdDev (N=3) | Variance  |
| ----------------- | ------------ | ---------- | ------------ | --------- |
| Example.Minimal() | 55.006299 ms | 56.1542 ms | 3.170436 ms  | 10.051667 |

### CSV

```csv
DisplayName,Median,Mean,StdDev,Variance,LowerOutliers,UpperOutliers,TotalNumOutliers,SampleSize,RawExecutionResults
MinimalTestExample.Minimal(),56.1542,55.006299,3.170436,10.051667,0,3,56.1542,51.4218,57.4429
```
