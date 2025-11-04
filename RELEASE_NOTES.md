## What's Changed in vNEXT_VERSION

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
