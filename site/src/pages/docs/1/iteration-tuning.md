---
title: Iteration Tuning (Operations per Invoke)
---

## Overview

Iteration tuning helps microbenchmarks reach a suitable per-iteration duration by executing multiple operations within a single measured iteration. This amortizes timer overhead and improves stability of very small operations. Each measured iteration is normalized to per-operation time, so reported statistics always reflect the cost of a single operation regardless of the batch size.

- OperationsPerInvoke (OPI): number of inner operations executed per measured iteration
- TargetIterationDurationMs: target duration (milliseconds) for each measured iteration
- Backward compatible: tuning is disabled by default and only runs when a target is provided

## How it works

When TargetIterationDurationMs > 0 and OperationsPerInvoke ≤ 1 (default), Sailfish runs a short pilot to estimate the per-operation time and then chooses an OPI to bring each measured iteration near the target.

Algorithm (simplified):
- Warm up the method
- Measure several single-operation samples, compute median per-op time
- Initial OPI ≈ round(targetMs / perOpMs), clamped to [1, Max]
- Refine up to two times by measuring the aggregate duration of OPI operations and proportionally adjusting toward the target (stop if within ~±20%)

If you explicitly set OperationsPerInvoke > 1, the tuner will not run even if a target is provided. Leave OperationsPerInvoke at its default (1) to allow auto‑tuning.

## Usage examples

```csharp
[Sailfish(TargetIterationDurationMs = 5)] // auto‑tune OPI to reach ~5 ms/iteration
public class MicroBench { ... }

[Sailfish(OperationsPerInvoke = 8)] // fixed OPI, no tuning
public class FixedBatch { ... }

[Sailfish(OperationsPerInvoke = 1, TargetIterationDurationMs = 10)] // enable tuning
public class Tuned { ... }
```

## Tips

- Choose targets between 2–10 ms for microbenchmarks; slower ops can use larger targets
- For unstable environments (e.g., CI VMs), prefer slightly larger targets for robustness
- Tuning is fast (a few pilot measurements) and runs per test method

## FAQ

- What if my operation is slower than the target? OPI will remain 1.
- Does tuning change my reported numbers? Reported statistics are **per-operation**. When OperationsPerInvoke > 1, Sailfish divides each measured iteration by OPI, so results represent the cost of a single operation and stay comparable regardless of the OPI chosen (manually or by the tuner).
- How do I disable it? Set TargetIterationDurationMs = 0 (the default) or explicitly specify OperationsPerInvoke > 1.

