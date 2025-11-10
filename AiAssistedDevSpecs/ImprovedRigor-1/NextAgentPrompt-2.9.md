# NextAgentPrompt-2.9 — Golden Output Tests for Consolidated Markdown and CSV

Add snapshot/golden tests that lock in the consolidated session Markdown and CSV formats produced at the end of a run. These will prevent regressions in headings, sections, NxN comparison matrices (BH-FDR + ratio CIs), and metadata blocks (health, manifest, timing).

## Required reading (absolute paths)
- G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/Sailfish_iPhone_Level_Polish_PRD.md (section 13 — updated checklist)
- G:/code/Sailfish/source/Sailfish/DefaultHandlers/Sailfish/MethodComparisonTestRunCompletedHandler.cs
- G:/code/Sailfish/source/Sailfish/DefaultHandlers/Sailfish/CsvTestRunCompletedHandler.cs
- G:/code/Sailfish/site/src/pages/docs/1/markdown-output.md
- G:/code/Sailfish/site/src/pages/docs/1/method-comparisons.md
- G:/code/Sailfish/site/src/pages/docs/1/reproducibility-manifest.md
- G:/code/Sailfish/source/Tests.Library/E2EScenarios/DefaultHandlerTests.cs (current smoke checks)

## Objective
Create deterministic, high-signal golden tests that:
- Generate the consolidated session Markdown content and compare to a checked-in golden file (after normalizing dynamic fields).
- Generate the consolidated session CSV content and compare to a checked-in golden file (after normalization).
- Cover NxN comparison matrix rendering with BH-FDR adjusted q-values, ratio CIs and labels (Improved/Similar/Slower), plus session metadata blocks (Environment Health, Reproducibility Manifest summary, Timer calibration snapshot if present).

## Non-goals
- Do not change runtime behavior or formatting; this task only adds tests and golden files.
- Do not introduce flaky time- or environment-dependent assertions; normalize dynamic values.

## Scope
1) Deterministic input fixtures
- Craft 2–3 minimal test classes with enough compiled results to exercise:
  - One comparison group with 3 methods (A,B,C) producing distinct means/SE so BH-FDR yields at least one “Improved” and one “Similar”.
  - At least one non-comparison method so the “Individual Test Results” section appears.
- Use fixed RunSettings values: `Seed=12345`, a fixed `TimeStamp`, and stable `Tags`.
- Build `IClassExecutionSummary` lists using existing builders or minimal mocks; use `PerformanceRunResultBuilder` or create direct `PerformanceRunResult` instances with deterministic arrays.

2) Normalization helpers (crucial)
- Implement a small test-only helper to normalize dynamic content prior to diffing:
  - Replace timestamps/IDs: e.g., `Session_2025-..` → `Session_<TS>`, GUID fragments → `<ID>`.
  - Paths: replace absolute directories with `<OUTDIR>`.
  - Numbers with minor floating error: round to fixed decimals where useful (e.g., "{value:F3}").
  - Manifest fields that vary (CI provider, CPU name) → keep only labels that are stable or redact to placeholders.
- Apply the same normalization for Markdown and CSV so tests are cross-platform and stable (line endings normalized to \n).

3) Golden files and locations
- Add resources under: `source/Tests.Library/TestResources/Golden/`
  - `ConsolidatedSession.md`
  - `ConsolidatedSession.csv`
- The tests should load these files, normalize both expected and actual, and assert equality.
- If a golden file is missing, the test should write the current output to disk and fail with a clear update instruction.

4) Tests to add
- File: `source/Tests.Library/Presentation/MarkdownOutputGoldenTests.cs`
  - Arrange: fabricate a notification payload to drive `MethodComparisonTestRunCompletedHandler`.
  - Capture the emitted `WriteMethodComparisonMarkdownNotification` content.
  - Normalize → compare to `Golden/ConsolidatedSession.md`.
- File: `source/Tests.Library/Presentation/CsvOutputGoldenTests.cs`
  - Arrange: fabricate a notification for `CsvTestRunCompletedHandler` using the same summaries.
  - Capture the emitted `WriteMethodComparisonCsvNotification` content.
  - Normalize → compare to `Golden/ConsolidatedSession.csv`.

5) Coverage expectations (what the goldens must include)
- Markdown
  - Session header with seed and timestamp (normalized), environment health summary line, reproducibility manifest summary block.
  - “Individual Test Results” table (at least one row).
  - “Performance Comparison Matrix” for a group with 3 methods (A,B,C) including:
    - Ratio, 95% CI, label Improved/Similar/Slower.
    - BH-FDR adjusted q-values shown or referenced per current format.
- CSV
  - Header row including: TestClass, TestMethod, MeanTime, MedianTime, StdDev, SampleSize, ComparisonGroup, Status.
  - Rows for all methods including the group labels.

## Design sketch (high level)
- Reuse existing handler code-paths instead of calling low-level formatters directly to keep goldens aligned with real outputs.
- Inject a logger and mediator; intercept notifications with a test mediator stub to capture the generated Markdown/CSV strings.
- Ensure any randomization is locked by `Seed=12345` and that test data produces stable ordering.
- Normalization helper can live in `source/Tests.Library/TestUtils/GoldenNormalization.cs`.

## Steps
1) Add deterministic fixtures and a builder method that returns `List<IClassExecutionSummary>` containing:
   - One class with comparison group `GroupX` and methods `A,B,C` with configured means (e.g., 100, 110, 102) and SEs to yield at least one significant improvement.
   - One class with a non-comparison method.
2) Add GoldenNormalization helper with:
   - Line-ending normalization to `\n`.
   - Regex replacements for timestamps (`20\d{2}-\d{2}-\d{2}T[\d:.-]+Z` → `<TS>`), session IDs, absolute paths, and any GUID substrings.
3) Implement `MarkdownOutputGoldenTests` and `CsvOutputGoldenTests` using handler invocation + mediator capture.
4) Create the `Golden/*.md` and `Golden/*.csv` files based on the first successful run, then re-run to lock.
5) Document the update flow in test failure messages: "If the format change is intentional, update the golden file at ...".

## Acceptance criteria
- New tests pass reliably on repeat runs and CI (no flakiness across OS/line endings).
- The goldens include NxN matrix with BH-FDR and ratio CIs, with at least one “Improved” and one “Similar” label.
- Session metadata sections (health, manifest summary) are present and normalized for dynamic values.
- CSV contains the documented columns and rows for all test cases.
- No changes to runtime code paths; only test code and resource files are added.

## Verification commands (safe, targeted)
- cd /d G:/code/Sailfish && cd   // confirm CWD
- dotnet build source/Sailfish.sln -c Debug -v:m
- dotnet test "G:/code/Sailfish/source/Tests.Library/Tests.Library.csproj" -c Debug --filter FullyQualifiedName~Golden -m:1

## File manifest to add
- source/Tests.Library/Presentation/MarkdownOutputGoldenTests.cs
- source/Tests.Library/Presentation/CsvOutputGoldenTests.cs
- source/Tests.Library/TestUtils/GoldenNormalization.cs (helper)
- source/Tests.Library/TestResources/Golden/ConsolidatedSession.md
- source/Tests.Library/TestResources/Golden/ConsolidatedSession.csv

## Notes
- Prefer invariant formatting (CultureInfo.InvariantCulture) when producing and normalizing numeric values.
- Keep normalization targeted to avoid hiding real regressions; limit redactions to known dynamic tokens (timestamps, paths, GUIDs) and numeric rounding where necessary.
- If existing docs differ slightly, let the goldens reflect the current behavior; open a follow-up doc-sync task only if discrepancies are material.

