# HANDOFF_SUMMARY-2.6 — Seeded Randomized Run Order (Tier A)

Status: COMPLETE (feature + tests + docs updates)

## What changed in this step
- Implemented deterministic, seeded run ordering (opt-in)
  - Ordering applied to: test classes, methods within classes, and variable-set combinations
  - API: `RunSettingsBuilder.WithSeed(int)`
  - Back-compat: Args-based keys (`seed`, `randomseed`, `rng`) still honored when `Seed` is null
  - Manifest + Markdown: Seed persisted in Reproducibility Manifest and shown in headers as `Seed: <value>`
- Fixed markdown formatting to match tests (plain `Seed: 123` vs bold)
- Kept `.WithSeed(...)` as the preferred path while preserving legacy Args behavior

## Files updated in this step
- README
  - Path: G:/code/Sailfish/README.md
  - Updates:
    - Features: added bullet for Seeded run order (opt-in)
    - New section: "Reproducible Run Order (Seed)" with `.WithSeed(...)` example and Args fallback note
- Release Notes
  - Path: G:/code/Sailfish/RELEASE_NOTES.md
  - Updates:
    - Added section: "New: Seeded Randomized Run Order (opt-in)" with API, determinism scope, manifest/markdown integration, and tests

## Test and build verification (targeted, safe)
- CWD: G:/code/Sailfish
- Commands (no demo perf tests):
  - `dotnet test G:/code/Sailfish/source/Tests.Library/Tests.Library.csproj -c Debug --filter TestCategory!=Slow`
  - `dotnet test G:/code/Sailfish/source/Tests.TestAdapter/Tests.TestAdapter.csproj -c Debug --filter TestCategory!=Slow`
  - `dotnet test G:/code/Sailfish/source/Tests.Analyzers/Tests.Analyzers.csproj -c Debug --filter TestCategory!=Slow`
- Outcome: All targeted suites passed

## Next agent starting point (NEXT FEATURE)
- Prompt: G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.6.md
- Ground rules:
  - Always `cd /d G:/code/Sailfish` then `cd` to confirm CWD before running commands
  - Only run targeted test projects (Library, TestAdapter, Analyzers); do NOT run demo perf tests

## Suggested next work (per roadmap)
1) Precision/Time Budget Controller (Tier A) — integrate opt‑in controller with Adaptive Sampling to relax thresholds under tight time budgets
2) OPI + TargetIterationDuration auto‑tuning (design reserved in Overhead V2 plan)
3) SailDiff runtime input pipeline (accept in‑memory objects)

## Verification (copy‑paste snippets for the next step)
```bash
cd /d G:/code/Sailfish
cd & rem verify cwd

dotnet build source/Sailfish.sln -c Debug -v:m

dotnet test "G:/code/Sailfish/source/Tests.Library/Tests.Library.csproj" -c Debug --filter TestCategory!=Slow -m:1

dotnet test "G:/code/Sailfish/source/Tests.TestAdapter/Tests.TestAdapter.csproj" -c Debug --filter TestCategory!=Slow -m:1

dotnet test "G:/code/Sailfish/source/Tests.Analyzers/Tests.Analyzers.csproj" -c Debug --filter TestCategory!=Slow -m:1
```
