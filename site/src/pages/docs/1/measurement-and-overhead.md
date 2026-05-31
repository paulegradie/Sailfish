---
title: Measurement & Overhead
---

## Overview

This page explains how Sailfish takes a single timed measurement and how it accounts for the small amount of overhead the harness itself adds. Understanding this helps you reason about how small a difference Sailfish can reliably resolve.

Sailfish runs in-process (no per-benchmark child process). The two things that determine the measurement noise floor are therefore (1) how the timed method is invoked and (2) how harness overhead is estimated and removed.

## How the timed method is invoked

The method under test is invoked through a **compiled, direct-call delegate** — not reflection.

When a test instance is constructed, Sailfish compiles a small delegate bound to that instance and method. Every supported return shape — `void`, `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, with or without a `CancellationToken` parameter — is normalized to a single `Func<CancellationToken, ValueTask>` shape. The compiled delegate is reused across all warmup, tuning, and measured invocations.

This means each measured call is a direct (often inlinable) method call with:

- **no `MethodInfo.Invoke`** dispatch per call,
- **no per-call argument array** (and no boxing of the `CancellationToken`),
- the `await` happening inside the timed region, so asynchronous work is included in the measurement.

Reflection is still used for test discovery and for lifecycle methods (`SailfishGlobalSetup`, `SailfishMethodSetup`, `SailfishIterationSetup`, and their teardowns) — none of which are timed.

## How overhead is measured and subtracted

Even a direct delegate call has a tiny, irreducible cost (the delegate invocation, awaiting an already-completed `ValueTask`, and the `Stopwatch` itself). Sailfish estimates this by timing an **idle delegate of the identical shape** — an empty `Func<CancellationToken, ValueTask>` — through the exact same loop the workload runs in, then subtracts the median from each measured iteration.

Because the baseline is *structurally identical* to the measured path (same delegate shape, same await, same loop), the subtraction cancels harness overhead almost exactly rather than approximating it. This is the same principle BenchmarkDotNet uses when it subtracts its generated overhead loop.

Overhead diagnostics (baseline ticks, drift, and capped-iteration count) are surfaced in the test Output window and the reproducibility manifest. With the compiled path, the measured overhead is typically near zero, so the drift signal is gated on an absolute-time floor to avoid reporting timer-quantization noise as drift.

{% callout title="Disabling overhead estimation" type="note" %}
You can turn off overhead estimation per class (`[Sailfish(DisableOverheadEstimation = true)]`), per method (`[SailfishMethod(DisableOverheadEstimation = true)]`), or globally (`RunSettingsBuilder.DisableOverheadEstimation()`). With the compiled invocation path the calibration cost is small, so leaving it on is usually fine.
{% /callout %}

## Before / after

Per-invocation harness overhead, measured on .NET 10 with the repository's `NoiseFloorBench` harness (an empty method, batched per-op timing):

| Metric                          | Reflection path | Compiled delegate |
| ---                             | ---             | ---               |
| Overhead floor (median)         | ~40 ns          | ~1.5 ns           |
| Overhead noise (std-dev)        | ~0.8 ns         | ~0.06 ns          |
| Allocation per call             | 272 B           | 0 B               |

Absolute values are machine- and runtime-specific, but the relationship is stable: the compiled path has a much lower and more consistent floor, and allocates nothing per call. (Note: on .NET 10 bare `MethodInfo.Invoke` dispatch is only ~6 ns; most of the legacy 40 ns was per-call bookkeeping — building an argument list, reading type names, and an error-message string — that the compiled path removes entirely.)

## What this means for resolving small differences

The overhead floor is subtracted from every sample, so its size and its run-to-run noise bound the smallest difference you can trust. A lower, more stable floor shifts that bound down, so Sailfish can distinguish smaller effects (and tighter confidence intervals on fast methods).

Zero per-call allocation matters just as much: per-call allocations eventually trigger garbage collections that appear as tail outliers, which widen distributions and corrupt small-difference tests. Removing them makes the per-iteration samples cleaner.

For very fast methods you can still combine this with [Iteration Tuning](/docs/1/iteration-tuning) (`OperationsPerInvoke`) to push each measured iteration above timer resolution.

{% callout title="Behavior change: async methods are awaited by return type" type="note" %}
The compiled invoker decides whether to await based on the method's **return type**. A method that returns a `Task` or `ValueTask` *without* the `async` keyword (for example `public Task Work() => DoAsync();`) is now correctly awaited and included in the measurement. Previously such a method could fall through the synchronous path and be under-measured. Methods written with `async`/`await` are unaffected.
{% /callout %}
