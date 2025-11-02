# Next agent prompt: Update docs, website, and release notes for multi‑level Confidence Intervals (95% + 99%)

Use this as copy‑paste instructions to update the Sailfish documentation website and release notes for the new multi‑level CI feature.

## Context and goals

We added a centralized confidence interval (CI) system that computes multiple levels by default (95% and 99%) and carries them through the pipeline for display in:
- Test Output Window (console/IDE)
- Markdown summaries
- CSV output

CI margins are formatted with adaptive precision (try 4 decimals → if zero, try 6 → then 8 → then show “0”). Backwards compatibility is preserved (legacy single CI still populated), but surfaces now show multiple CIs when available.

Your job: Update the website docs and our release notes to reflect these changes. Do not change code.

## Repository and important paths

- Repo root: G:\code\Sailfish
- Docs website (Next.js + Markdoc): G:\code\Sailfish\site\
- Doc pages to update:
  - G:\code\Sailfish\site\src\pages\docs\2\sailfish.md
  - G:\code\Sailfish\site\src\pages\docs\1\output-attributes.md
  - G:\code\Sailfish\site\src\pages\docs\1\csv-output.md
  - G:\code\Sailfish\site\src\pages\docs\0\essential-information.md
  - New page to add: G:\code\Sailfish\site\src\pages\docs\1\confidence-intervals.md
- Release notes:
  - G:\code\Sailfish\RELEASE_NOTES_TEMPLATE.md (reference)
  - G:\code\Sailfish\RELEASE_NOTES.md (create or overwrite for this release)
  - Website “release notes” page (pointer to GitHub Releases): G:\code\Sailfish\site\src\pages\docs\4\releasenotes.md

## What changed (authoritative summary for docs)

- New ConfidenceIntervalResult model: holds ConfidenceLevel, MarginOfError, Lower, Upper.
- PerformanceRunResult now has ConfidenceIntervals: IReadOnlyList<ConfidenceIntervalResult>.
- ExecutionSettings has ReportConfidenceLevels (default [0.95, 0.99]); primary legacy ConfidenceLevel remains (default 0.95).
- All surfaces updated:
  - Console/IDE output: shows a row per CI level ("95% CI ±", "99% CI ±"), using adaptive precision.
  - Markdown: appends a compact CI summary per test (e.g., "95% CI ± 11.0496ms, 99% CI ± 14.9900ms").
  - CSV: added columns CI95_MOE and CI99_MOE.
- Note: RunSettingsBuilder currently does not expose a convenience method to set ReportConfidenceLevels; document defaults and consider advanced/programmatic mention only if necessary.

## Tasks

1) Update “Sailfish” overview page to show multiple CI rows in Output
- File: G:\code\Sailfish\site\src\pages\docs\2\sailfish.md
- In the “Outputs” section:
  - Add two CI rows under the descriptive statistics example to demonstrate 95% and 99% CI margin-of-error.
  - Add a brief, plain-English note on what a CI represents and that 99% is wider than 95%.
  - Mention adaptive precision for CI margin formatting.
  - Tip: Note that the ± symbol may appear as a replacement character in some terminal encodings; this is cosmetic.

Example snippet (shortened):
```text
| Mean     |     55.3240 |
| Median   |     55.3000 |
| 95% CI ± |     11.0496 |
| 99% CI ± |     14.9900 |
```

2) Update Output Attributes page (Markdown + CSV features) to mention multi‑CI
- File: G:\code\Sailfish\site\src\pages\docs\1\output-attributes.md
- Under “Markdown Output Features” add a bullet:
  - "Displays multiple confidence intervals (95% and 99% by default) with adaptive precision."
- Under “CSV Output Features” add a bullet:
  - "Includes CI95_MOE and CI99_MOE columns for margin-of-error at 95% and 99% confidence."

3) Update CSV Output page for schema and examples
- File: G:\code\Sailfish\site\src\pages\docs\1\csv-output.md
- In “Section 2: Individual Test Results”:
  - Update the header and examples to include CI95_MOE and CI99_MOE.
  - Add field descriptions for CI95_MOE and CI99_MOE: "Margin of error (ms) at 95%/99% confidence; computed using Student’s t distribution and standard error of the mean. Precision is adaptively formatted in output displays; CSV stores numeric values."

Example updated header/row:
```csv
# Individual Test Results
TestClass,TestMethod,MeanTime,MedianTime,StdDev,SampleSize,ComparisonGroup,Status,CI95_MOE,CI99_MOE
PerformanceTest,BubbleSort,45.2000,44.1000,3.1000,100,Algorithms,Success,1.2345,1.6789
```

4) Add a new “Confidence Intervals” explainer page
- New file: G:\code\Sailfish\site\src\pages\docs\1\confidence-intervals.md
- Purpose: Briefly explain:
  - What a CI means in plain English
  - 95% vs 99% trade-offs (99% is wider, more conservative; may reduce sensitivity to small changes)
  - Sailfish uses Student’s t distribution with degrees of freedom n-1 and the sample’s standard error
  - Defaults: Reports 95% and 99% CI; formatting uses adaptive precision for readability
  - Where users will see CIs: console, markdown, CSV
- Optional: Include a short example tying N=24, Mean 55.324 ms, 95% CI ± 11.0496 ms, 99% CI ± ~14.99 ms.

Example mini-block:
```text
Plain-English: If you repeated the experiment many times, 95% of such intervals would contain the true average runtime. 99% is wider (more conservative).
```

5) Update “Essential Information” with a short CI blurb
- File: G:\code\Sailfish\site\src\pages\docs\0\essential-information.md
- Add a short section: "Sailfish reports 95% and 99% confidence intervals by default for the mean runtime; see Confidence Intervals for details."

6) Release notes
- Create/update: G:\code\Sailfish\RELEASE_NOTES.md
- Use the template’s vNEXT_VERSION placeholder. Include bullets below.
- Keep the website’s release notes page as a pointer to GitHub Releases; optionally append a one-liner under “Why the Move?” noting the new multi‑CI feature.

Suggested content for RELEASE_NOTES.md:
```markdown
## What's Changed in vNEXT_VERSION

- New: Centralized multi-level Confidence Intervals (CI)
  - Default reporting includes 95% and 99% CI for mean runtime
  - Uses Student’s t distribution (df = n-1) with standard error
  - Preserves legacy single-CI fields for backward compatibility
- Output improvements
  - Console/IDE: shows a row per CI level ("95% CI ±", "99% CI ±") with adaptive precision (4→6→8 decimals, else 0)
  - Markdown: concise CI summary per test (e.g., "95% CI ± 11.0496ms, 99% CI ± 14.9900ms")
  - CSV: new columns CI95_MOE and CI99_MOE
- Docs: Updated pages for CSV schema, output attributes, and a new “Confidence Intervals” explainer
```

7) Validation checklist
- Run the docs site locally to confirm pages build and render (from G:\code\Sailfish\site\):
  - npm install (if needed)
  - npm run dev (or npm run build)
  - Verify pages:
    - /docs/2/sailfish
    - /docs/1/output-attributes
    - /docs/1/csv-output
    - /docs/1/confidence-intervals
    - /docs/0/essential-information
- Scan pages for “±” rendering; confirm it appears correctly; note any encoding substitution in terminals.
- No code changes expected; unit tests unaffected; optional: dotnet test as a quick smoke.

## Acceptance criteria

- Sailfish overview (“Outputs”) displays 95% and 99% CI rows and explains them succinctly.
- Output Attributes page mentions multi-CI in both Markdown and CSV features.
- CSV Output page includes CI95_MOE and CI99_MOE in schema and examples, with brief field descriptions.
- New Confidence Intervals page exists and explains 95% vs 99% in plain English, how Sailfish computes CI, and where users see it.
- Essential Information page mentions that CI reporting is included by default with a link to the explainer.
- RELEASE_NOTES.md created/updated with the suggested content (vNEXT_VERSION placeholder intact).
- Site builds without errors; content links work.

## Notes and guardrails

- Do not document a public configuration API for setting ReportConfidenceLevels yet unless a documented pathway is added. Default is [0.95, 0.99]. If you mention advanced configuration, keep it minimal and clearly “advanced/programmatic.”
- Keep examples small and focused; avoid fragile whitespace/encoding examples.
- CSV examples should include the two new CI columns but remain concise.

## Useful references (for orientation; no code edits needed)

Console formatter shows multiple CI rows using adaptive formatting:
```csharp
foreach (var ci in results.ConfidenceIntervals.OrderBy(x => x.ConfidenceLevel))
    momentTable.Add(new Row(FormatAdaptive(ci.MarginOfError), $"{ci.ConfidenceLevel:P0} CI ±"));
```

CSV map adds CI columns:
```csharp
Map(m => m.CI95MarginOfError).Name("CI95_MOE");
Map(m => m.CI99MarginOfError).Name("CI99_MOE");
```

Markdown adds CI summary lines:
```csharp
var ciParts = pr.ConfidenceIntervals.OrderBy(ci => ci.ConfidenceLevel)
    .Select(ci => $"{ci.ConfidenceLevel:P0} CI ± {FormatAdaptive(ci.MarginOfError)}ms");
```

