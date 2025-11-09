# Next Agent Prompt — Phase 2: Final Polish and Follow‑ups

Current Date: 2025-11-09
Current Branch: pg/complete-improv
Working Directory: G:/code/Sailfish/source

## Context (What’s done in this pass)
Phase 2 “iPhone‑level polish” progressed with method comparison rigor and CSV parity:
- NxN method comparisons (adapter + consolidated markdown) now use Benjamini–Hochberg FDR–adjusted q‑values and 95% ratio confidence intervals (computed on the log scale)
- Session CSV output brought to parity and remains backward compatible:
  - Added columns: ComparisonGroup, Method1, Method2, Mean1, Mean2, Ratio, CI95_Lower, CI95_Upper, q_value, Label
  - Kept legacy ChangeDescription column (Improved/Regressed/No Change) for existing parsers
  - Standard error computed from StdDev and sample size when not present in tracking format
- TestAdapter comparison markdown gained a "Detailed Results" table to satisfy existing tests and improve clarity
- Targeted test runs are green (see commands below); Performance demo projects were NOT executed

Key references:
- PHASE2_QUICK_START: G:/code/Sailfish/PHASE2_QUICK_START.md
- Release Notes: G:/code/Sailfish/RELEASE_NOTES.md
- CSV Handler: G:/code/Sailfish/source/Sailfish/DefaultHandlers/Sailfish/CsvTestRunCompletedHandler.cs
- Adapter Comparison Processor: G:/code/Sailfish/source/Sailfish.TestAdapter/Queue/Processors/MethodComparison/MethodComparisonProcessor.cs

## Ground rules
- Always run `pwd` after changing directories; commands assume: `G:/code/Sailfish/source`
- Only run tests for core library and adapter; do NOT execute performance demo projects
- Maintain backward compatibility (CSV keeps ChangeDescription)

## Quick verification (safe)
```bash
cd /d G:\code\Sailfish\source
cd & rem verify cwd

dotnet build Sailfish.sln -nologo -v:m

rem Targeted adapter test proving markdown change
.dotnet\dotnet.exe test "G:\code\Sailfish\source\Tests.TestAdapter\Tests.TestAdapter.csproj" --no-build \
  --filter FullyQualifiedName~GenerateMarkdownIfRequested_WithWriteToMarkdown_PublishesMarkdownNotification -m:1 \
  --logger "trx;LogFileName=Tests.TestAdapter.Markdown.trx"

rem Targeted library CSV tests
.dotnet\dotnet.exe test "G:\code\Sailfish\source\Tests.Library\Tests.Library.csproj" --no-build \
  --filter FullyQualifiedName~CsvTestRunCompletedHandlerTests -m:1 \
  --logger "trx;LogFileName=Tests.Library.Csv.trx"
```

## Proposed next work streams (pick one)
1) Seeded randomized run order (persist seed)
- Goal: Deterministic ordering across runs; surface seed in manifest and outputs
- Acceptance: Seed used for method ordering; persisted in reproducibility manifest; displayed in logs/markdown

2) OperationsPerInvoke + TargetIterationDuration auto‑tuning
- Goal: Automatically choose OPI and target durations based on pilot data
- Acceptance: Converges to sensible OPI/target duration; logged decisions; no regressions in tests

3) Precision/Time budgets controller
- Goal: Integrate precision/time budgets with adaptive sampling for predictable run times
- Acceptance: Budgets enforced; early stop logic integrated; tests cover edge cases

4) SailDiff runtime input support (no file dependency)
- Goal: Accept in‑memory comparison data for TestAdapter scenarios in addition to JSON files
- Acceptance: Public API accepts objects and files; clear messaging when companion tests were not run

If unsure, start with (1) Seeded run order, then (2) OPI/TargetIterationDuration.

## Definition of done
- All existing tests pass; add focused tests for new behavior
- No changes to performance demo projects
- Docs updated (site pages + release notes if user‑facing)
- PHASE2_QUICK_START updated only if entry points change
- CSV retains legacy ChangeDescription alongside new Label

## Notes
- Adapter discovery behaves differently in IDE; tests rely on AppContext.BaseDirectory for robust file discovery
- Anti‑DCE helper is available: Sailfish.Utilities.Consumer.Consume<T>(...)

