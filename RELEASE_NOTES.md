## What's Changed in vNEXT_VERSION
- New: NxN Method Comparisons rigor + CSV parity
  - Test Adapter and session markdown: NxN comparisons per group with Benjamini–Hochberg FDR–adjusted q-values and 95% ratio confidence intervals (computed on the log scale)
  - CSV session output updated to include: ComparisonGroup, Method1, Method2, Mean1, Mean2, Ratio, CI95_Lower, CI95_Upper, q_value, Label, ChangeDescription (legacy column retained for backward compatibility)
  - Label uses Improved/Slower/Similar; legacy ChangeDescription retains Improved/Regressed/No Change for existing parsers
  - Standard error computed from StdDev and sample size when not present in tracking format
  - Comparison markdown now includes a "Detailed Results" table to satisfy existing tests and improve clarity


- New: Environment Health Check (default: enabled)
  - Validates the host at test run start; computes a 0–100 score with a label (Excellent/Good/Fair/Poor)
  - Publishes a summary in INF/DBG logs and now appends it to the bottom of each test’s Output window
  - Checks include: Build Mode, JIT (Tiered/OSR), Process Priority, GC Mode, CPU Affinity, High‑Resolution Timer, Power Plan, Background CPU load
  - Global toggle: `RunSettingsBuilder.WithEnvironmentHealthCheck(false)` to disable
  - Docs: See `/docs/1/environment-health`
  - Timer check now reports effective sleep granularity (median of Thread.Sleep(1)) across Windows/macOS/Linux, so short sleeps show as ~OS tick length in outputs
  - Build Mode warns in Debug (recommend Release optimizations); JIT details include COMPlus_TieredCompilation, COMPlus_TC_QuickJit, COMPlus_TC_QuickJitForLoops, and COMPlus_TC_OnStackReplacement


- New: Reproducibility Manifest (best-effort)
  - Captures environment metadata: .NET runtime, OS and architecture, CPU model (best-effort), GC mode, JIT flags (Tiered/QuickJit/OSR), process priority, CPU affinity, and high-resolution timer
  - Records Environment Health score/label plus session info (timestamp, session ID, tags, CI)
  - Includes per-method snapshots: N, warmups, mean, StdDev, and CI margins (95%/99%)
  - Persisted as `Manifest_<timestamp>.json` in the Run Settings output directory (default: `sailfish_default_output`; Test Adapter can override via `.sailfish.json` -> `GlobalSettings.ResultsDirectory`)
  - Markdown includes a short "Reproducibility Summary" near the top when available
  - Docs: See `/docs/1/reproducibility-manifest`


- New: Anti‑DCE Consumer API
  - Add `Sailfish.Utilities.Consumer.Consume<T>(...)` to discourage dead‑code elimination in hot paths during benchmarking
  - Marked NoInlining; uses Volatile.Write + GC.KeepAlive
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
