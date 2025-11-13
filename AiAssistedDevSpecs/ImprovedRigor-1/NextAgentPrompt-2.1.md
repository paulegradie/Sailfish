# Next Agent Prompt — Phase 2 Wrap-up and Next Steps

Current Date: 2025-11-09
Current Branch: pg/complete-improv
Working Directory: G:/code/Sailfish/source

## Context (What's done)
Phase 2 improvements are complete and verified green for targeted test projects (core library + test adapter). Do not run performance demo projects.

Completed in Phase 2:
- Validation Warnings (IDE output + markdown)
- Environment Health Check additions: Build Mode + JIT detection
- Reproducibility Manifest (JSON persisted + markdown summary)
- Anti-DCE Consumer API (`Sailfish.Utilities.Consumer.Consume<T>(...)`)
- Docs and release notes updated accordingly

Key references:
- PHASE2_QUICK_START: G:/code/Sailfish/PHASE2_QUICK_START.md
- Release Notes: G:/code/Sailfish/RELEASE_NOTES.md
- Docs pages:
  - Environment Health: G:/code/Sailfish/site/src/pages/docs/1/environment-health.md
  - Reproducibility Manifest: G:/code/Sailfish/site/src/pages/docs/1/reproducibility-manifest.md
  - Markdown Output: G:/code/Sailfish/site/src/pages/docs/1/markdown-output.md
  - Anti-DCE Consumer: G:/code/Sailfish/site/src/pages/docs/1/anti-dce.md

## Ground rules
- Always run `pwd` after changing directories; commands assume: `G:/code/Sailfish/source`
- Only run tests for core library and adapter; do not execute performance demo projects
- Keep backward compatibility; avoid breaking public APIs unless explicitly planned

## Quick verification (safe)
```bash
cd /d G:\code\Sailfish\source
cd & rem verify cwd

dotnet build -nologo -c Release

dotnet test Tests.Library/Tests.Library.csproj -nologo -c Release -m:1

dotnet test Tests.TestAdapter/Tests.TestAdapter.csproj -nologo -c Release -m:1
```

## Proposed next work streams (pick one)
1) Markdown consolidation for method comparisons (NxN matrices)
- Goal: One consolidated markdown per session with NxN method comparison tables for method groups across classes
- Acceptance hints:
  - Single session file contains all `[WriteToMarkdown]` results
  - NxN tables per method group; clear 'improved' vs regress language
  - Works in IDE test adapter runs and CLI
- Useful context: AiAssistedDevSpecs/TestAdapterQueueRearchitect/OutputFormatting/Markdown/

2) SailDiff runtime input support (no file dependency)
- Goal: Allow comparison module to accept in-memory data objects (test adapter scenario) in addition to JSON files
- Acceptance hints:
  - Public API accepts objects and files
  - Clear messaging in test output when comparisons can't run because a companion test wasn't executed

3) Overhead estimation/neutralization polish
- Goal: Improve overhead estimator to better neutralize system overhead in runs; surface overhead diagnostics in outputs
- Acceptance hints:
  - Reproducible diagnostics appear in IDE output + markdown
  - Estimator math and CI fields correctly populated in all `PerformanceRunResult` constructions

4) Queue system decoupling follow-ups
- Goal: Ensure TestCaseCompleted notifications are queued and post-processed before final reporting to framework
- Acceptance hints:
  - Decoupled pipeline maintains behavior with improved reliability

If unsure, start with (1) Markdown consolidation, as it improves day-to-day UX and builds on the current docs.

## Definition of done (for your chosen task)
- All existing tests pass; add focused tests for new behavior
- No changes to performance demo projects
- Docs updated (site pages + release notes if user-facing)
- PHASE2_QUICK_START handoff section updated only if the next phase changes entry points

## Useful files to read first
- G:/code/Sailfish/PHASE2_QUICK_START.md
- G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/Sailfish_iPhone_Level_Polish_PRD.md

## Notes
- Adapter discovery can behave differently in IDE; tests rely on `AppContext.BaseDirectory` for robust file discovery
- Anti-DCE helper is available: `Sailfish.Utilities.Consumer.Consume<T>(...)`

## Completion checklist
- [ ] Build succeeds in Release
- [ ] Tests.Library and Tests.TestAdapter green
- [ ] New/updated docs committed
- [ ] Provide a brief summary + exact file paths changed

NEXT AGENT SETUP:
Please start here: G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.1.md

This file contains your prompt and starting context for the next steps.

