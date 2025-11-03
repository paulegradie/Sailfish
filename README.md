# Sailfish

[![Build Pipeline v2.2](https://github.com/paulegradie/Sailfish/actions/workflows/build-v2.2.yml/badge.svg)](https://github.com/paulegradie/Sailfish/actions/workflows/build-v2.2.yml)
![NuGet](https://img.shields.io/nuget/dt/Sailfish)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=sailfish_library&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=sailfish_library)
[![codecov](https://codecov.io/gh/paulegradie/Sailfish/graph/badge.svg?token=UN17VRVD0N)](https://codecov.io/gh/paulegradie/Sailfish)

Sailfish is a .NET performance testing framework that makes it easy to write, run, and analyze performance tests with statistical rigor.

- Documentation: https://paulgradie.com/Sailfish/
- NuGet: https://www.nuget.org/packages/Sailfish/

---

## Table of Contents
- [Features](#features)
- [Quick Start](#quick-start)
- [Algorithm Comparisons](#algorithm-comparisons)
- [Adaptive Sampling](#adaptive-sampling)
- [Installation](#installation)
- [Outputs and Reporting](#outputs-and-reporting)
- [Used By](#used-by)
- [Contributing](#contributing)
- [License](#license)

---

## Features
- Statistical rigor: outlier detection, multiple tests, significance analysis
- Method comparisons with `[SailfishComparison]`
- Multiple outputs: test logs, Markdown, CSV
- Easy CI/CD integration
- Historical analysis with SailDiff
- Highly configurable

## Quick Start

```csharp
[Sailfish]
public class MyPerformanceTest
{
    [SailfishMethod]
    public void TestMethod()
    {
        // Your code to test
        Thread.Sleep(10);
    }
}
```

## Algorithm Comparisons
Compare multiple algorithms automatically and get N×N matrices, significance tests, and clear performance ratios.

```csharp
[WriteToMarkdown]  // Generate consolidated markdown output
[WriteToCsv]       // Generate consolidated CSV output
[Sailfish(SampleSize = 100)]
public class AlgorithmComparison
{
    [SailfishMethod]
    [SailfishComparison("SortingAlgorithms")]
    public void BubbleSort() { /* implementation */ }

    [SailfishMethod]
    [SailfishComparison("SortingAlgorithms")]
    public void QuickSort() { /* implementation */ }
}
```

## Adaptive Sampling
Adaptive sampling stops collecting samples once results are statistically stable (defaults shown):
- Coefficient of Variation (CV) ≤ 5%
- Relative Confidence Interval width ≤ 20% at 95% confidence

Attribute-based (per-class):
```csharp
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05, MaximumSampleSize = 1000)]
public class StableTiming
{
    [SailfishMethod]
    public async Task Work() => await Task.Delay(10);
}
```

Global configuration (all tests in a run):
```csharp
var runSettings = RunSettingsBuilder.CreateBuilder()
    .WithGlobalAdaptiveSampling(targetCoefficientOfVariation: 0.05, maximumSampleSize: 500)
    // other options like .WithGlobalSampleSize(...), .DisableOverheadEstimation(), etc.
    .Build();
```

Note: Global settings act as defaults/overrides and can be combined with attribute settings per class.

## Installation
Install via NuGet:

```bash
dotnet add package Sailfish
```

## Outputs and Reporting
- N×N method comparison matrices per comparison group
- Statistical significance testing (p-values, confidence intervals)
- Consolidated Markdown and CSV outputs
- Clear performance ratios (e.g., "X times faster/slower")

See the full documentation for output details and examples: https://paulgradie.com/Sailfish/

## Used By
<p>
  <img src="./assets/OctopusDeploy-logo-RGB.svg" alt="Octopus Deploy" width="320" />
</p>
<p>
  <img src="./assets/empower.svg" alt="Empower" width="320" />
</p>

## Contributing
Contributions are welcome! Feel free to open issues and pull requests.

## License
This project is licensed. See the LICENSE file in the repository for details.
