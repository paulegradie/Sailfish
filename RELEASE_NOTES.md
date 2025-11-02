## What's Changed in vNEXT_VERSION

- New: Centralized multi-level Confidence Intervals (CI)
  - Default reporting includes 95% and 99% CI for mean runtime
  - Uses Student’s t distribution (df = n-1) with standard error
  - Preserves legacy single-CI fields for backward compatibility
- Output improvements
  - Console/IDE: shows a row per CI level ("95% CI ±", "99% CI ±") with adaptive precision (4→6→8 decimals, else 0)
  - Markdown: concise CI summary per test (e.g., "95% CI ± 11.0496ms, 99% CI ± 14.9900ms")
  - CSV: new columns CI95_MOE and CI99_MOE
- Docs: Updated pages for CSV schema, output attributes, and a new "Confidence Intervals" explainer
