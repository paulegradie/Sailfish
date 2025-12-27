---
title: Test Output Window
---

## Overview

When you run Sailfish tests through an IDE test runner (for example Visual Studio Test Explorer), Sailfish writes a rich, text-only summary into the **Test Output** pane for each test case.

This page explains what you will see there and how it relates to SailDiff and other diagnostics.

## Sailfish per-test summaries

For standard Sailfish runs (without SailDiff enabled), Sailfish writes a per-test summary that matches the console-style table output:

- Descriptive statistics (N, Mean, Median)
- Confidence-interval rows for each configured confidence level
- Min/Max values after outliers are removed
- A list of upper/lower outliers (if any)
- The full distribution of cleaned samples
- Validation warnings when the run health is questionable (for example, very noisy environment or low sample size)

The underlying implementation in the test adapter is `SailfishConsoleWindowFormatter`. It formats a markdown-like table and sends it to the test framework writer so you can read it directly in the Test Output window.

## SailDiff analysis output

When **SailDiff** is enabled for a run, the Test Output window also receives the result of the statistical comparison:

- A **"SAILDIFF PERFORMANCE ANALYSIS"** header with an impact summary such as "🟢 IMPACT: 15% faster (IMPROVED)" or "🔴 IMPACT: 20% slower (REGRESSED)".
- The list of *before* and *after* tracking IDs that were compared.
- Details of the statistical test: test type, alpha, and p-value.
- A "Change" line that explains whether the change is significant and why.
- A compact table comparing mean, median, and sample size before vs. after.

If the statistical test fails (for example because there is not enough data to run the test), SailDiff writes a short error message instead of the full table so you can see why the result is missing.

## Runtime diagnostics in the Test Output window

Sailfish also appends short diagnostics that help you reason about environment quality:

- Overhead calibration: baseline ticks, drift %, and capped-iteration count
- Timer granularity notes when the effective sleep resolution is coarse (for example Windows ~15.6 ms)
- Environment health warnings (when enabled) summarising jitter and other signals

These diagnostics are mirrored between:

- Console/INF logs during `dotnet test`
- The IDE Test Output window for each test case
- Consolidated markdown outputs, which may include an Environment Health section and reproducibility summary

For details on markdown and CSV outputs, see:

- [Markdown](/docs/1/markdown-output)
- [Csv](/docs/1/csv-output)

