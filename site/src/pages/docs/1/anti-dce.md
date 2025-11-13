---
title: Anti‑DCE Consumer
---

Preventing the JIT from eliminating “dead” work is important when you benchmark hot paths. Sailfish provides a tiny helper that introduces low‑cost, observable side effects so your measurements aren’t accidentally optimized away.

## When to use it
- Microbenchmarks or inner loops where results aren’t otherwise observed
- After computing a value you don’t actually need in production code inside the benchmark body
- As a final statement in a measured operation to ensure the JIT can’t prove the work is useless

Avoid using it in regular production code; it’s a benchmarking aid.

## API
- Namespace: `Sailfish.Utilities`
- Method: `Consumer.Consume<T>(T value)`

Example:

```csharp
using Sailfish.Utilities;

public void MyHotPath()
{
    var result = DoWork();
    Consumer.Consume(result); // discourage dead‑code elimination
}
```

## What it does
Consumer.Consume is implemented with three guard rails:
- Marked `NoInlining` to prevent method inlining
- Uses `Volatile.Write` to store the value to a static location (introduces a memory barrier)
- Uses `GC.KeepAlive` so the value (and any referenced object graph) is considered live

These effects are cheap and observable enough to make JIT elimination unlikely while having negligible impact on timing.

## Notes and guidance
- Prefer placing `Consumer.Consume(...)` at the edge of the hot path rather than around every intermediate step
- For value types, boxing is fine here; the goal is to preserve work, not to be allocation‑free
- This is not a silver bullet—compilers evolve—but it mirrors the proven pattern used by BenchmarkDotNet’s consumer

## Related
- Environment Health: [/docs/1/environment-health](/docs/1/environment-health)
- Reproducibility Manifest: [/docs/1/reproducibility-manifest](/docs/1/reproducibility-manifest)



## Anti‑DCE Analyzers

Static analysis rules that help you avoid dead‑code elimination risks in benchmarks. Each rule includes a one‑click code fix.

- SF1001 – Unused return value inside [SailfishMethod]
  - Warns when a non‑void call result is ignored. Fix wraps the call with `Consumer.Consume(...)`.
  - Before → `DoWork();`  After → `Consumer.Consume(DoWork());`
- SF1002 – Constant‑only computation in [SailfishMethod]
  - Warns on arithmetic that uses only constants/literals. Fix appends `Consumer.Consume((expr));` to make work observable.
  - Example: `var x = 2 * 1024;` becomes `var x = 2 * 1024; Consumer.Consume((2 * 1024));`
- SF1003 – Empty loop body inside [SailfishMethod]
  - Warns on hot loops that perform no observable work. Fix inserts `Consumer.Consume(0);` into the loop body.

Notes:
- These analyzers run only inside methods marked with `[SailfishMethod]`.
- You can suppress per occurrence with `#pragma warning disable/restore` or via an `.editorconfig` severity setting if needed.
