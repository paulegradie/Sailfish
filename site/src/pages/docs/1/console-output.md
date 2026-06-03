---
title: Reading the Console Output
---

Every Sailfish run prints a per-test report to **StdOut** (and to the **Test Output** window in your IDE). For a `[Sailfish]` class with a baseline, you also get a **method-comparison** block and an **environment health** summary. This page walks through a complete run end-to-end so you know how to read every line — including the inline distribution plots.

Here is a full run of a two-method comparison — `WithPlus` (the baseline) vs. `WithJoin` — measured in nanoseconds:

{% terminal title="CompareCpuAlgorithms ▸ dotnet test" %}
```
CompareCpuAlgorithms.WithJoin

Descriptive Statistics
----------------------
| Stat     |  Time (ns) |
| ---      | ---        |
| N        |        188 |
| Mean     |    575.814 |
| Median   |    542.000 |
| 95% CI ± |     9.0287 |
| 99% CI ± |    11.9105 |
| Min      |    458.000 |
| Max      |    709.000 |


Distribution Plot
-----------------
                                                           Time (ns)

                 ▁▁▁   ▃▃▃▄▄▄███▅▅▅▅▃▃▃▂▂▂   ▃▃▃   ▃▃▃▃
                             ╿    ╵

    400 ├──────────────┬──────────────┬─────────────┬──────────────┤ 800
                      500            600           700

  ╵ mean   ╿ median   ▁▂▃▄▅▆▇█ count per bin

        n=188  outliers=12  min=458.000  max=709.000

Outliers Removed (12)
---------------------
12 Upper Outliers: 750.000, 1000.000, 1000.000, 750.000, 792.000, 1208.000, 750.000, 750.000, 792.000, 792.000, 750.000, 750.000

Distribution (ns)
-----------------
625.000, 667.000, 625.000, 541.000, 542.000, 542.000, 500.000, 500.000, 542.000, 667.000, 541.000, 584.000, 667.000, 625.000, 584.000, 625.000, 708.000, 583.000, 541.000, 542.000, 500.000, 541.000, … (188 values total)

📊 PERFORMANCE COMPARISON
Group: Concat
==================================================

🟢 IMPACT: WithJoin(N: 100) is 80.5% faster than baseline WithPlus(N: 100) (IMPROVED)
   P-Value: 0.000000 | Mean: 2.952 µs → 0.576 µs


📋 DETAILED STATISTICS:

| Metric      | WithPlus(N: 100) (baseline) | WithJoin(N: 100) | Change | P-Value  |
| ----------- | --------------------------- | ---------------- | ------ | -------- |
| Mean (µs)   | 2.952                       | 0.576            | -80.5% | 0.000000 |
| Median (µs) | 2.958                       | 0.542            | -81.7% | -        |

Change = comparison vs. baseline (positive = slower, negative = faster).

Statistical Test: Two-Sample Wilcoxon Signed-Rank Test
Alpha Level: 0.0001
Sample Size: 188
Outliers Removed: 18

📊 DISTRIBUTION
                                                                       Time (µs)

  WithPlus(N: 100)                 ▇▇▇▇▂▂▂▁▁▁▁▄▄▄███▂▂▂▂▃▃▃▂▂▂▂▂▂▂▁▁▁▁▁▁▁▁▁▁▁
                                                 ╽
  WithJoin(N: 100)       ▃▂▃
                         ╿╵

                  0 ├───────────────────┬──────────────────┬───────────────────┤ 6
                                        2                  4

  ╵ mean   ╿ median   ▁▂▃▄▅▆▇█ count per bin   ╽ mean≈median

  WithPlus(N: 100)  n=188  outliers=0  min=1.500  max=5.708
  WithJoin(N: 100)  n=188  outliers=0  min=0.458  max=0.709

==================================================

Sailfish Environment Health: 64/100 (Fair)
 - Build Mode: Warn (Debug) - Use Release (optimized) for stable measurements
 - JIT (Tiered/OSR): Pass (Tiered=default; QuickJit=default; QuickJitForLoops=default; OSR=default)
 - Process Priority: Warn (Normal) - Consider High or AboveNormal to reduce scheduler noise
 - GC Mode: Warn (Workstation GC) - Enable Server GC for more stable throughput measurements
 - CPU Affinity: Unknown (Not supported on this OS)
 - Timer: Pass (High-resolution timer: ~1 ns; Sleep(1) median ≈ 1.1 ms)
```
{% /terminal %}

That's a lot at a glance — so let's read it one section at a time.

## 1. Descriptive statistics

Every test case opens with a small stats table, headed by the test's display name (`CompareCpuAlgorithms.WithJoin`). The unit in the column header (`Time (ns)`) is chosen automatically to keep the numbers readable.

{% terminal title="Descriptive Statistics" %}
```
Descriptive Statistics
----------------------
| Stat     |  Time (ns) |
| ---      | ---        |
| N        |        188 |
| Mean     |    575.814 |
| Median   |    542.000 |
| 95% CI ± |     9.0287 |
| 99% CI ± |    11.9105 |
| Min      |    458.000 |
| Max      |    709.000 |
```
{% /terminal %}

| Row | Meaning |
| --- | --- |
| **N** | Number of measurements kept **after** outlier removal — this is the sample the stats are computed from. |
| **Mean** | Arithmetic average of the retained measurements. |
| **Median** | Middle value. When it sits well below the mean (as here, 542 vs. 576) the distribution is **right-skewed** — a few slow runs are pulling the mean up. |
| **95% CI ± / 99% CI ±** | Half-width of the confidence interval on the **mean**. "Mean 575.814 with 95% CI ± 9.0287" means the true mean is very likely within `575.814 ± 9.0287` ns. Tighter is better. |
| **Min / Max** | Fastest and slowest retained measurement. |

{% callout title="Why prefer the median?" type="note" %}
For microbenchmarks the **median** is usually the number to trust — it's robust to the occasional slow run that the OS scheduler or GC injects. The gap between mean and median is itself a signal: a large gap means a skewed distribution, which the plot below makes visible. See [Confidence Intervals](/docs/1/confidence-intervals) for how the CI is computed.
{% /callout %}

## 2. The distribution plot

Numbers summarize; the plot shows the **shape**. Sailfish draws a compact, text-only distribution directly in the output so you never have to leave the console to see how your measurements are spread.

{% terminal title="Distribution Plot" %}
```
Distribution Plot
-----------------
                                                           Time (ns)

                 ▁▁▁   ▃▃▃▄▄▄███▅▅▅▅▃▃▃▂▂▂   ▃▃▃   ▃▃▃▃
                             ╿    ╵

    400 ├──────────────┬──────────────┬─────────────┬──────────────┤ 800
                      500            600           700

  ╵ mean   ╿ median   ▁▂▃▄▅▆▇█ count per bin

        n=188  outliers=12  min=458.000  max=709.000
```
{% /terminal %}

How to read it:

- **The bars** are a histogram. The x-axis is time; each column is a bin, and its height (`▁▂▃▄▅▆▇█`, eight levels) is **how many measurements landed in that bin**. The tall `███` cluster just past 500 ns is where most runs landed.
- **`╿` marks the median** and **`╵` marks the mean**, placed under the axis at their true positions. Here `╿` (median ≈ 542) sits left of `╵` (mean ≈ 576) — the same right-skew the stats table hinted at, now visible as a long thin tail stretching toward 700+.
- **The axis** is labelled at both ends (`400` … `800`) with interior ticks (`500`, `600`, `700`) so you can read values off the bars.
- **The footer** repeats the essentials: `n` (retained count), `outliers` removed, and the `min`/`max` of the retained sample.

{% callout title="Box plot or histogram" type="note" %}
The plot above is the **Histogram** style. The default is a compact **box plot** (quartile box with whiskers, mean, and median). Switch styles, or turn the inline plot off entirely:

```csharp
var runSettings = RunSettingsBuilder.CreateBuilder()
    .WithDistributionPlotStyle(DistributionPlotStyle.Histogram) // default: BoxPlot
    .WithDistributionPlots(true)                                // false to hide the plot
    .Build();
```
{% /callout %}

## 3. Outliers removed

Before computing statistics, Sailfish detects and **sets aside** outliers so a handful of scheduler hiccups don't distort your mean. They're reported, never silently dropped — you can always see exactly what was excluded and why.

{% terminal title="Outliers Removed" %}
```
Outliers Removed (12)
---------------------
12 Upper Outliers: 750.000, 1000.000, 1000.000, 750.000, 792.000, 1208.000, 750.000, 750.000, 792.000, 792.000, 750.000, 750.000
```
{% /terminal %}

The count in the header (`12`) is how many measurements were excluded; they're listed split into **Upper** (slow) and **Lower** (fast) outliers. Notice the values here — 750 to 1208 ns — sit far above the ~542 ns median, exactly the kind of slow-path noise you want kept out of the summary. See [Outlier Handling](/docs/1/outlier-handling) to tune the detection method and bounds.

## 4. The retained distribution

For full transparency, Sailfish then prints the **actual retained measurements** — the exact sample the statistics and plot were built from (outliers already removed).

{% terminal title="Distribution (ns)" %}
```
Distribution (ns)
-----------------
625.000, 667.000, 625.000, 541.000, 542.000, 542.000, 500.000, 500.000, 542.000, 667.000, … (188 values total)
```
{% /terminal %}

This is what makes a Sailfish result reproducible and auditable: paste it into a notebook, re-run your own stats, or diff it against a previous run. (The full list is printed; it's truncated here for space.)

## 5. The performance comparison (SailDiff)

When a `[Sailfish]` class has a baseline, **SailDiff** compares each method against it and prints a verdict block. This is the part you actually act on.

{% terminal title="Performance Comparison" %}
```
📊 PERFORMANCE COMPARISON
Group: Concat
==================================================

🟢 IMPACT: WithJoin(N: 100) is 80.5% faster than baseline WithPlus(N: 100) (IMPROVED)
   P-Value: 0.000000 | Mean: 2.952 µs → 0.576 µs


📋 DETAILED STATISTICS:

| Metric      | WithPlus(N: 100) (baseline) | WithJoin(N: 100) | Change | P-Value  |
| ----------- | --------------------------- | ---------------- | ------ | -------- |
| Mean (µs)   | 2.952                       | 0.576            | -80.5% | 0.000000 |
| Median (µs) | 2.958                       | 0.542            | -81.7% | -        |

Change = comparison vs. baseline (positive = slower, negative = faster).

Statistical Test: Two-Sample Wilcoxon Signed-Rank Test
Alpha Level: 0.0001
Sample Size: 188
Outliers Removed: 18
```
{% /terminal %}

Reading top to bottom:

- **`Group: Concat`** — the comparison group these methods belong to.
- **The `IMPACT` line** is the headline verdict, always phrased as *"{compared} is N% slower/faster than baseline {primary}"*. The colored dot and tag tell you the direction at a glance: 🟢 `IMPROVED`, 🔴 `REGRESSED`, or ⚪ `NOT SIGNIFICANT` when the difference can't be distinguished from noise.
- **`P-Value: 0.000000`** — the probability this difference is due to chance. Below the **Alpha Level** (here `0.0001`) it's significant; well below it, as here, it's a confident result.
- **`Mean: 2.952 µs → 0.576 µs`** — the before/after means, in the comparison's chosen unit (µs here, because the two methods together span microseconds).
- **The `DETAILED STATISTICS` table** breaks the change out by **Mean** and **Median**. `Change` is signed relative to the baseline: **negative = faster**, positive = slower. Both rows agreeing (−80.5% / −81.7%) is a strong, consistent improvement.
- **The test footer** names the statistical test (a **Two-Sample Wilcoxon Signed-Rank Test** — non-parametric, robust to skew), the alpha level, the sample size, and how many outliers were removed for the comparison.

{% callout title="More on the verdict" type="note" %}
SailDiff's full mechanics — baseline vs. N×N modes, the Mann-Whitney/Wilcoxon/t-test options, BH-FDR q-values, and the ratio confidence intervals written to Markdown/CSV — are covered on the [SailDiff](/docs/2/saildiff) page.
{% /callout %}

## 6. The comparison distribution plot

SailDiff also overlays both methods on a **shared axis**, so the improvement isn't just a number — you can see the entire `WithJoin` distribution sitting to the left of `WithPlus`, with no overlap.

{% terminal title="Comparison Distribution" %}
```
📊 DISTRIBUTION
                                                                       Time (µs)

  WithPlus(N: 100)                 ▇▇▇▇▂▂▂▁▁▁▁▄▄▄███▂▂▂▂▃▃▃▂▂▂▂▂▂▂▁▁▁▁▁▁▁▁▁▁▁
                                                 ╽
  WithJoin(N: 100)       ▃▂▃
                         ╿╵

                  0 ├───────────────────┬──────────────────┬───────────────────┤ 6
                                        2                  4

  ╵ mean   ╿ median   ▁▂▃▄▅▆▇█ count per bin   ╽ mean≈median

  WithPlus(N: 100)  n=188  outliers=0  min=1.500  max=5.708
  WithJoin(N: 100)  n=188  outliers=0  min=0.458  max=0.709
```
{% /terminal %}

Both rows share **one x-axis** (`0` … `6` µs), so position is directly comparable: `WithJoin`'s tiny cluster near 0.5 µs is visibly faster than `WithPlus`'s spread around 3 µs. The same `╿` median / `╵` mean markers appear under each row; the extra **`╽` marks where mean and median coincide** — handy for a tight, symmetric distribution like `WithPlus` here. Each row gets its own `n / outliers / min / max` footer.

## 7. Environment health

Finally, Sailfish grades the **machine it ran on**. Microbenchmarks are sensitive to build mode, GC, process priority, and timer resolution — this 0–100 score tells you how much to trust the numbers above.

{% terminal title="Environment Health" %}
```
Sailfish Environment Health: 64/100 (Fair)
 - Build Mode: Warn (Debug) - Use Release (optimized) for stable measurements
 - JIT (Tiered/OSR): Pass (Tiered=default; QuickJit=default; QuickJitForLoops=default; OSR=default)
 - Process Priority: Warn (Normal) - Consider High or AboveNormal to reduce scheduler noise
 - GC Mode: Warn (Workstation GC) - Enable Server GC for more stable throughput measurements
 - CPU Affinity: Unknown (Not supported on this OS)
 - Timer: Pass (High-resolution timer: ~1 ns; Sleep(1) median ≈ 1.1 ms)
```
{% /terminal %}

Each check is `Pass`, `Warn`, `Fail`, or `Unknown` (when the OS can't report it — `CPU Affinity` on macOS here), with an actionable hint. A score of **64 (Fair)** is a nudge: this run was in `Debug` with Workstation GC, so treat the absolute numbers as indicative. The fix is in the messages — build in `Release`, enable Server GC — and the [Environment Health Check](/docs/1/environment-health) page explains every check and how to disable it.

## Where this output shows up

- **Console / `dotnet test`** — printed to StdOut as the run proceeds.
- **IDE Test Output window** — the same per-test report appears under each test in Rider / Visual Studio.
- **Consolidated Markdown & CSV** — add `[WriteToMarkdown]` / `[WriteToCsv]` to capture comparison tables and stats to disk. See [Markdown Output](/docs/1/markdown-output) and [CSV Output](/docs/1/csv-output).

{% callout title="Tune what you see" type="note" %}
Most of this output is configurable on the run settings:

- **Distribution plot** — `WithDistributionPlotStyle(...)` (box plot / histogram), `WithDistributionPlots(false)` to hide it.
- **Outliers** — see [Outlier Handling](/docs/1/outlier-handling).
- **Confidence intervals** — see [Confidence Intervals](/docs/1/confidence-intervals).
- **Environment health** — `WithEnvironmentHealthCheck(false)` to silence it. See [Environment Health Check](/docs/1/environment-health).
- **Comparison verdict & test** — see [SailDiff](/docs/2/saildiff).
{% /callout %}
