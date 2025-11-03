## What's Changed in vNEXT_VERSION

- New: Adaptive Sampling execution strategy
  - Opt-in via attribute per class or globally via RunSettingsBuilder
  - Converges based on Coefficient of Variation (CV) threshold and optional relative CI width
  - Honors MinimumSampleSize and MaximumSampleSize safety caps
  - Backward compatible: fixed sampling remains default
  - Docs: See /docs/1/adaptive-sampling and ADAPTIVE_SAMPLING_MIGRATION_GUIDE.md

- Output improvements
  - Sample size (N) moved to the top of the descriptive statistics table
  - Multi-level Confidence Intervals (CI): default reporting includes 95% and 99%
  - Adaptive precision for CI formatting (try 4 → 6 → 8 decimals; shows 0 if still zero)
  - Markdown now includes a compact multi-CI summary; CSV adds CI95_MOE and CI99_MOE

- Public API additions (non-breaking)
  - SailfishAttribute: UseAdaptiveSampling, TargetCoefficientOfVariation, MinimumSampleSize, MaximumSampleSize, ConfidenceLevel
  - IRunSettings: GlobalUseAdaptiveSampling?, GlobalTargetCoefficientOfVariation?, GlobalMaximumSampleSize?
  - RunSettingsBuilder: WithGlobalAdaptiveSampling(double targetCoefficientOfVariation, int maximumSampleSize)

- Method comparison and results consistency
  - Test adapter console shows multiple CI rows when available
  - Comparison pipeline computes CI fields where missing to keep outputs consistent

- Documentation & website
  - New docs page: /docs/1/adaptive-sampling (why, how, configuration, migration)
  - Homepage “What’s New” announcement and Quick Link
  - Cross-links from Getting Started, Essential Information, and When to Use Sailfish

- Demos & examples
  - New demo tests under source/PerformanceTests/AdaptiveSamplingDemos showcasing convergence, strict thresholds, and edge cases

- Breaking changes
  - None expected; changes are additive. Note: internal test adapter Row type widened from double to object for display flexibility.
