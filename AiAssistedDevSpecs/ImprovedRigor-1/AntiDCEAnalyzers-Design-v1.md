# Anti‑DCE Analyzers — Design v1

Scope: Introduce analyzers (and code fixes) that help authors prevent dead‑code elimination from skewing Sailfish performance tests. Targets .NET 8/9. Ship as Sailfish.Analyzers with tests and documentation.

## Goals
- Catch common patterns where the JIT may eliminate or fold work inside `[SailfishMethod]` bodies
- Provide actionable guidance and safe code fixes (prefer `Consumer.Consume(...)`)
- Keep false positives low; allow opt‑outs via pragmas and `.editorconfig`

## Rules (initial set)

### SF1001: Unobserved result in benchmark method
- Trigger: In a `[SailfishMethod]`, the last produced value (or intermediate results in hot path) is never observed, assigned to a wider scope, or passed to a known sink (e.g., `Consumer.Consume`, logging, volatile write).
- Examples (diagnostic):
  - `var x = Compute();` and `x` not used subsequently
  - `return;` after computing a value that is never read
- Safe sinks (not flagged):
  - `Sailfish.Utilities.Consumer.Consume(x)`
  - Assignment to `volatile` field
  - Passing to methods with `[DoesNotReturn]` or known side‑effect sinks (configurable list)
- Message: "Computed value is not observed; JIT may eliminate the work in benchmarks. Consider Consumer.Consume(value)."
- Severity: Warning
- Fix: Wrap expression in `Consumer.Consume(...)` or assign to dedicated volatile field

### SF1002: Constant‑foldable computation in measured code
- Trigger: Obvious constant expressions or pure operations on literals/constants inside `[SailfishMethod]` loop/hot path (compiler could pre‑compute).
- Examples (diagnostic):
  - `for (var i = 0; i < N; i++) { var y = 2 * 1024; ... }`
  - `var a = new[] { 1, 2, 3 }; var z = a.Where(v => v > 1).Count();` when `a` is constant and `z` lifetime unused
- Message: "Code in the measured region appears constant‑foldable; results may not reflect real work."
- Severity: Info (configurable)
- Fix: Move constant setup outside the hot path or consume the result explicitly

### SF1003: Hot loop without observable work
- Trigger: Tight loops in `[SailfishMethod]` with no writes/consumption to observable sinks in the loop body.
- Examples (diagnostic):
  - `for (...) { total += F(i); }` where `total` is local and not observed later
  - `foreach (...) { _ = F(item); }` with the result unused
- Message: "Loop body performs work that may be eliminated. Consider consuming results or adding an observable sink."
- Severity: Warning
- Fix: Add `Consumer.Consume(result)` in the loop or accumulate into a `volatile`/returned value

## Configuration
- `.editorconfig` keys (defaults in package):
  - `dotnet_diagnostic.SF1001.severity = warning`
  - `dotnet_diagnostic.SF1002.severity = suggestion`
  - `dotnet_diagnostic.SF1003.severity = warning`
- Suppression: standard `#pragma warning disable SF100x` or `SuppressMessage`
- Known sinks list extensible via additional file (future)

## Architecture & Heuristics
- Use Roslyn analyzers (Microsoft.CodeAnalysis)
- Detect `[SailfishMethod]` via attribute symbol match; limit scope to those methods
- Data‑flow analysis to detect unused values and loop bodies without side effects
- Recognize `Sailfish.Utilities.Consumer.Consume` as a sink (plus volatile writes, ref/out params)
- Avoid FP: treat logging, IO, interlocked ops, and method calls with unknown side effects as sinks

## Code Fix Providers
- SF1001/SF1003: Insert `Consumer.Consume(expr)` with `using Sailfish.Utilities;`
- SF1002: Move constant computation outside loop or add consumption; offer preview diff

## Testing Strategy
- Analyzer unit tests for positive/negative cases; verify diagnostics locations/messages
- Code fix tests asserting resulting code compiles and removes diagnostics
- Performance tests for analyzer execution on representative Sailfish test projects

## Packaging & Wiring
- New project: `source/Sailfish.Analyzers/Sailfish.Analyzers.csproj`
- Tests: `source/Tests.Analyzers/Tests.Analyzers.csproj`
- Include analyzers in the main NuGet via `PackageReference IncludeAssets=analyzers`

## Implementation Steps
1) Scaffold projects + CI step to run analyzer tests
2) Implement SF1001 with tests + code fix
3) Implement SF1002 with tests + code fix
4) Implement SF1003 with tests + code fix
5) Wire analyzers into Sailfish NuGet and validate in sample projects
6) Update docs page `/docs/1/anti-dce` with rules, examples, and suppression guidance

## Acceptance Criteria
- All analyzer tests pass in CI
- No new warnings in Sailfish codebase under default severity
- Documentation updated; release notes mention analyzers

