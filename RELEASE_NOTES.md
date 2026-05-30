## What's Changed in v0.0.118 (unreleased)

### SailDiff statistical rigor (Tier 1)

**Breaking — default test type and alpha changed.** SailDiff's defaults have been wrong for the data Sailfish actually produces. Two related changes:

- **Default test is now `WilcoxonRankSumTest`** (Mann-Whitney U), the correct non-parametric test for two independent samples. The previous default, `TwoSampleWilcoxonSignedRankTest`, requires *paired* samples — each before[i] must be paired with after[i] by experimental design — which independent benchmark iterations are not. Using signed-rank on unpaired data violates the test's assumptions and produces invalid p-values. The signed-rank test remains available for callers who genuinely have paired data; its docstring now states this requirement explicitly.
- **Default alpha is now 0.05** (was 0.001), aligning with conventional statistical practice and with the 95% confidence intervals reported alongside each result. The previous 0.001 combined with `WilcoxonRankSumTest`'s exact null-distribution constraints made detection essentially impossible at typical Sailfish sample sizes — the core "comparisons aren't sensitive enough" complaint. Presets shift accordingly: `Default` 0.05, `Tight` 0.01, `Relaxed` 0.10.

**Mann-Whitney rewrite — uses your whole sample.** The Mann-Whitney wrapper previously down-sampled both sides to at most 10 observations, ran the test 25 times on different random subsamples, then voted (majority-significant wins) and reported the mean of either the significant or non-significant p-values. None of that is a valid statistical procedure and it destroyed power. The wrapper now runs the test once on the full preprocessed sample. The underlying `MannWhitneyDistribution` already supports both the exact null distribution (small N) and the tie-corrected normal approximation with continuity correction (large N); the exact path is now capped at ~2 × 10⁶ table entries to prevent OOM on samples around N ≈ 30 per side.

**Single source of truth for α.** Three independent paths used to apply a hardcoded `0.05` cutoff regardless of the user's configured alpha — the N×N comparison matrix in test-adapter markdown, in session markdown, and in CSV; the t-test's reported confidence interval (always 95%); and two `ToComparisonData` converters in the formatters. All four sites now thread the configured `SailDiffSettings.Alpha`, propagated either by direct settings injection or via the new `SailDiffSignificance.MetadataKey` per-message metadata. CI widths now follow `1 − α` (95% at α = 0.05, 99% at α = 0.01).

**Deterministic stats.** `TestPreprocessor.DownSampleWithRandomUniform` previously used `new Random()` (wall-clock seed) when no explicit seed was passed, so the down-sample was non-deterministic even when `RunSettingsBuilder.WithSeed` was set. The preprocessor now optionally takes `IRunSettings` via DI and consults `Seed`; the joint-sample form uses offset seed streams so identical-length samples don't draw identical index sets; and selected indices are sorted before extraction so output ordering doesn't depend on `HashSet<int>`'s undocumented enumeration order.

**Migration notes:**
- Code that constructs `new SailDiffSettings()` without arguments will see different significance verdicts. To keep the old behaviour explicitly: `new SailDiffSettings(alpha: 0.001, testType: TestType.TwoSampleWilcoxonSignedRankTest)`. We don't recommend this — it was incorrect — but the option is preserved.
- The previously-recorded MW "convergence" test that asserted `NoChange` for an effect of d ≈ 1.0 at N = 30 was effectively documenting the broken behaviour; it now uses a genuinely small effect to test the same property.
- A new internal helper, `Sailfish.Analysis.SailDiff.Statistics.SailDiffSignificance`, provides `IsSignificant(p, α)` / `IsSignificantPositive(q, α)` / `ReadFromMetadata(...)` for downstream code that needs a consistent cutoff.

## What's Changed in v0.0.117

- Fix: failed `[SailfishComparison]` members now publish `TestOutcome.Failed` instead of `TestOutcome.None`, so IDE Test Explorer surfaces the exception and stack trace correctly (#228, #230)
- Fix: comparison batch readiness no longer waits on members that failed — single-survivor groups complete cleanly and N×N matrices are computed across the surviving members only (#231)
- Fix: a single `[SailfishComparison]` test (no siblings) now reports its real outcome rather than `TestOutcome.None`
- New: `[SailfishMethodSetup]` / `[SailfishMethodTeardown]` gain a `RunOnce` flag — invoke a setup/teardown at most once per executor run for the declaring class, even when `MethodNames` lists multiple methods (#224)
- New: `SailfishPreset` recipes (`Default` / `Tight` / `Relaxed`) — `RunSettingsBuilder.WithPreset(SailfishPreset.Tight)` seeds adaptive sampling, outlier handling, and SailDiff defaults in one call; explicit `WithGlobalX` / `WithSailDiff` calls win over the preset, and `WithPreset` itself is last-wins when called multiple times (#218, #219)
- Build: drop `net8.0`; Sailfish, `Sailfish.TestAdapter`, and `Sailfish.Analyzers` now target `net9.0` and `net10.0` only (#226)
- Rider discovery: emit `TestCase.Hierarchy` + `ManagedType`/`ManagedMethod` properties and honor absolute `OutputPath` in `TestAdapterPath` so parameterized variants nest correctly under their parent method (#223, #225). Includes a documented one-time VSTest opt-in.
- TestAdapter robustness: `AdapterRunSettingsLoader` no longer silently swallows exceptions when `.sailfish.json` is malformed; failures surface as `TestClassInstantiationException` (#222).

## v0.0.116 and earlier
- New: NxN Method Comparisons rigor + CSV parity
  - Test Adapter and session markdown: NxN comparisons per group with Benjamini–Hochberg FDR–adjusted q-values and 95% ratio confidence intervals (computed on the log scale)
  - CSV session output updated to include: ComparisonGroup, Method1, Method2, Mean1, Mean2, Ratio, CI95_Lower, CI95_Upper, q_value, Label, ChangeDescription (legacy column retained for backward compatibility)
  - Label uses Improved/Slower/Similar; legacy ChangeDescription retains Improved/Regressed/No Change for existing parsers
  - Standard error computed from StdDev and sample size when not present in tracking format
  - Comparison markdown now includes a "Detailed Results" table to satisfy existing tests and improve clarity

- Internal: Golden/snapshot tests now lock the consolidated session Markdown and CSV formats
  - Normalization rules ensure cross‑TFM stability (works on .NET 8 and .NET 9)
  - Docs updated: `/docs/1/markdown-output`, `/docs/1/method-comparisons`, `/docs/1/csv-output`

- Internal: Build hygiene and analyzer/test stability
  - Solution builds with 0 warnings on .NET 8 and .NET 9
  - Roslyn analyzer release tracking added (AnalyzerReleases.Shipped.md/Unshipped.md)
  - Analyzer project targets netstandard2.0 and uses PrivateAssets=all; RS1038 suppressed only where required by design
  - Analyzer test harness pinned to Microsoft.CodeAnalysis 4.14.0 to eliminate MSB3277 conflicts; legacy CS0618 warnings suppressed in Tests.Analyzers
  - No runtime behavior changes; user-facing APIs unaffected



- New: Environment Health Check (default: enabled)
  - Validates the host at test run start; computes a 0–100 score with a label (Excellent/Good/Fair/Poor)
  - Publishes a summary in INF/DBG logs and now appends it to the bottom of each test’s Output window
  - Checks include: Build Mode, JIT (Tiered/OSR), Process Priority, GC Mode, CPU Affinity, High‑Resolution Timer, Power Plan, Background CPU load
  - Global toggle: `RunSettingsBuilder.WithEnvironmentHealthCheck(false)` to disable
  - Docs: See `/docs/1/environment-health`
  - Timer check now reports effective sleep granularity (median of Thread.Sleep(1)) across Windows/macOS/Linux, so short sleeps show as ~OS tick length in outputs
  - Build Mode warns in Debug (recommend Release optimizations); JIT details include COMPlus_TieredCompilation, COMPlus_TC_QuickJit, COMPlus_TC_QuickJitForLoops, and COMPlus_TC_OnStackReplacement


- New: Iteration Tuning (Operations per Invoke auto‑tuning)
  - Automatically tunes `OperationsPerInvoke` to reach a target per‑iteration duration when `TargetIterationDurationMs > 0`
  - Inert when `TargetIterationDurationMs == 0` or when an explicit `OperationsPerInvoke > 1` is set
  - Improves timer suitability and stability for microbenchmarks by batching multiple operations per measured iteration
  - Docs: See `/docs/1/iteration-tuning` and `/docs/1/required-attributes#time-budget--iteration-controls`



- New: Timer Calibration + Jitter Scoring (default: enabled)
  - Characterizes the high‑resolution timer and baseline overhead; computes dispersion (RSD%) and a 0–100 Jitter Score: `score = clamp(0,100, 100 − 4×RSD%)`
  - Surfaces in:
    - Enhanced Markdown header (Timer Calibration)
    - Reproducibility Manifest (`TimerCalibration` snapshot: StopwatchFrequency, ResolutionNs, BaselineOverheadTicks, Warmups, Samples, StdDevTicks, MedianTicks, RsdPercent, JitterScore)
    - Environment Health: adds “Timer Jitter” with thresholds — Pass ≤ 5%, Warn ≤ 15%, Fail > 15%
  - Toggle globally: `RunSettingsBuilder.WithTimerCalibration(false)`
  - Tests: +11 unit tests covering scoring, boundaries, manifest/markdown integration
  - Docs updated: README and docs site pages (/docs/1/environment-health, /docs/1/markdown-output, /docs/1/reproducibility-manifest)


- New: Reproducibility Manifest (best-effort)
  - Captures environment metadata: .NET runtime, OS and architecture, CPU model (best-effort), GC mode, JIT flags (Tiered/QuickJit/OSR), process priority, CPU affinity, and high-resolution timer
  - Records Environment Health score/label plus session info (timestamp, session ID, tags, CI)
  - Includes per-method snapshots: N, warmups, mean, StdDev, and CI margins (95%/99%)
  - Persisted as `Manifest_<timestamp>.json` in the Run Settings output directory (default: `sailfish_default_output`; Test Adapter can override via `.sailfish.json` -> `GlobalSettings.ResultsDirectory`)
  - Markdown includes a short "Reproducibility Summary" near the top when available
  - Docs: See `/docs/1/reproducibility-manifest`


- Reproducibility: Randomization Seed surfaced in consolidated markdown
  - The "Reproducibility Summary" now shows the Randomization Seed (when seeded randomized run order is enabled), improving repeatability of test sessions
  - Docs: See `/docs/1/markdown-output` and `/docs/1/reproducibility-manifest`

- New: SailDiff runtime input (in‑memory objects)
  - SailDiff can now analyze `TestData` in memory—ideal for Test Adapter and runtime scenarios
  - API: `ISailDiff.Analyze(TestData beforeData, TestData afterData, SailDiffSettings settings)`
  - Docs: See `/docs/2/saildiff`

- New: Precision/Time Budget Controller (opt-in)
  - Helps long-running tests finish within a per-method time budget by conservatively relaxing precision targets when enabled (backward compatible; off by default)
  - Enable (per class): `[Sailfish(UseTimeBudgetController = true, MaxMeasurementTimePerMethodMs = 30_000)]`
  - Behavior: after the pilot/minimum phase, estimates remaining budget and per-iteration cost; if tight, relaxes CV and CI targets within caps (CV ≤ 0.20; relative CI width ≤ 0.50); never tightens thresholds
  - Logging: `Budget controller: remaining={RemainingMs:F1}ms, est/iter={PerIterMs:F2}ms, TargetCV {OldCv:F3}->{NewCv:F3}, MaxCI {OldCi:F3}->{NewCi:F3}`
  - Docs: `/docs/1/precision-time-budget`


- New: Anti‑DCE Consumer API
  - Add `Sailfish.Utilities.Consumer.Consume<T>(...)` to discourage dead‑code elimination in hot paths during benchmarking
  - Marked NoInlining; uses Volatile.Write + GC.KeepAlive
  - Docs: See `/docs/1/anti-dce`


- New: Anti‑DCE Analyzers (with Code Fixes)
  - SF1001: Unused return value inside `[SailfishMethod]` — fix wraps the call with `Consumer.Consume(...)`
  - SF1002: Constant‑only computation detected — fix appends `Consumer.Consume((expr))`
  - SF1003: Empty loop body in hot path — fix inserts `Consumer.Consume(0);` into the loop body
  - Packaged with Sailfish: analyzers ship via the Sailfish NuGet (no extra install). Works in VS/Rider and supports Fix All.
  - Docs: See `/docs/1/anti-dce`


- UX: Runtime diagnostics in outputs
  - Per-test overhead diagnostics are now shown in both console logs and the IDE Test Output window (baseline ticks, drift %, and capped-iteration count; when disabled, a one-liner notes no subtraction)
  - Environment Health includes a timer entry with both high-resolution timer details and effective sleep granularity (median of Thread.Sleep(1))
  - Docs: See `/docs/1/output-attributes` and `/docs/1/environment-health#timer-granularity-and-short-sleeps`


- New: Configurable Outlier Handling (opt-in, typed)
  - Five strategies: RemoveUpper, RemoveLower, RemoveAll, DontRemove, Adaptive
  - Backward compatible: legacy behavior remains default unless explicitly enabled
  - Global override: `RunSettingsBuilder.WithGlobalOutlierHandling(useConfigurable: true, strategy: OutlierStrategy.RemoveUpper)`
  - Docs: See `/docs/1/outlier-handling`

- New: Adaptive Parameter Selection (pilot-based)
  - Classifies pilot samples into speed bands (UltraFast, Fast, Medium, Slow, VerySlow)
  - Tunes CV/CI budgets accordingly for faster, more reliable convergence
  - Integrated with AdaptiveIterationStrategy

  - Now also recommends a minimum sample size by speed category; AdaptiveIterationStrategy honors it as a local floor for convergence checks and logs it: "Adaptive tuning: {Category} -> MinN={MinN}, TargetCV={TargetCV:F3}, MaxCI={MaxCI:F3}"
- New: Statistical Validation Warnings
  - Emits warnings for: LOW_SAMPLE_SIZE, EXCESSIVE_OUTLIERS, HIGH_CV, WIDE_CI
  - Displayed in Markdown output alongside result tables

- Accuracy: Two-tailed t‑distribution table
  - Accurate critical values used by StatisticalConvergenceDetector
  - Replaces prior approximation logic

- Public API additions (non-breaking)
  - ExecutionSettings: `UseConfigurableOutlierDetection`, `OutlierStrategy`
  - IRunSettings/RunSettingsBuilder: global overrides via `WithGlobalOutlierHandling(...)`

- Documentation & website
  - README: Outlier handling overview + global config example
  - Docs site: new page `/docs/1/outlier-handling` (linked under Sailfish Basics)

- Tests
  - Unit tests for configurable outlier detection, adaptive selector, validator, and t‑distribution
  - Integration tests verifying legacy vs configurable outlier behaviors end‑to‑end

- Breaking changes
  - None. All features are opt‑in; defaults preserve legacy behavior.

### How to enable (global)

```csharp
var run = RunSettingsBuilder.CreateBuilder()
    .WithGlobalOutlierHandling(useConfigurable: true, strategy: OutlierStrategy.RemoveUpper)
    .Build();
```
- Diagnostic IDs: SF1001–SF1003 are the Anti‑DCE rules; global setup property analyzers renumbered to SF1013–SF1015 to avoid collisions.



### Technical Details

- Outlier detection is strategy-driven (opt-in) using Tukey-style fences; legacy path continues to remove both lower and upper outliers when not enabled
- Adaptive parameter selection performs a short pilot to classify speed and tune CV/CI budgets; gracefully falls back if classification fails
- Statistical validation evaluates sample sufficiency, outlier rates, coefficient of variation, and confidence interval width to surface actionable guidance
- Convergence now uses accurate two-tailed t critical values

### Migration Guide

No breaking changes; defaults preserve legacy behavior. To opt in:

1. Global enable configurable outlier handling

   ```csharp
   var run = RunSettingsBuilder.CreateBuilder()
       .WithGlobalOutlierHandling(useConfigurable: true, strategy: OutlierStrategy.RemoveUpper)
       .Build();
   ```
2. Review Markdown output for any validation warnings and adjust sampling parameters as needed
3. Optional: adopt adaptive sampling (if not already) to target precision budgets automatically

### Links

- Pull Request: https://github.com/paulegradie/Sailfish/pull/211
- Documentation: /docs/1/outlier-handling
