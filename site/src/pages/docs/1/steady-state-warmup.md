---
title: Steady-State Warmup
---

## Overview

By default Sailfish runs a fixed number of warmup iterations (`NumWarmupIterations`, default 3) before it starts measuring. For code whose performance shifts while the runtime tiers up (tiered JIT compilation and on-stack replacement), a fixed count can start measuring too early — capturing the tail of compilation — or waste time warming up longer than needed.

Steady-state warmup (opt-in) instead warms up **until the per-iteration timing stops trending and stabilizes**, bounded by a floor and a cap.

## How it works

Each warmup iteration is timed locally (it is *not* recorded into your measured samples) and fed to a detector. The detector looks at a sliding window of recent warmup durations and declares steady state when both:

- **The trend has flattened** — the median of the most recent half of the window is within ~5% of the median of the prior half (timings have stopped falling). Medians are used so a single cold-start spike doesn't dominate.
- **Dispersion is low** — the coefficient of variation across the window is within ~15%.

Warmup runs at least `NumWarmupIterations` (the floor) and at most `MaxWarmupIterations` (the cap, default 50), stopping as soon as steady state is detected. The detector needs a full window (6 samples) before it can decide, so that is the effective minimum.

## Usage

```csharp
[Sailfish(
    UseSteadyStateWarmup = true,  // opt in
    NumWarmupIterations = 4,      // floor: always warm up at least this many
    MaxWarmupIterations = 50,     // cap: never warm up more than this
    SampleSize = 100)]
public class MyBenchmarks
{
    [SailfishMethod]
    public void Work() { /* ... */ }
}
```

Configured per class on the `[Sailfish]` attribute (like `OperationsPerInvoke`). Defaults: `UseSteadyStateWarmup = false` (fixed-count warmup), `MaxWarmupIterations = 50`.

## Notes

- **Opt-in and backward compatible.** With the default (`false`), warmup is the existing fixed `NumWarmupIterations` loop — unchanged.
- **Bounded.** A noisy method whose timing never settles will warm up to `MaxWarmupIterations` and then proceed; it won't loop forever.
- The detector thresholds (window 6, ~5% drift, ~15% CV) are tuned internally and not currently exposed.
- This shapes *warmup* only; it's independent of [Adaptive Sampling](/docs/1/adaptive-sampling) (which decides how many *measured* samples to collect) and composes with it.
