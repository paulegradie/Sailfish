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



## Anti‑DCE Analyzers (coming soon)

Static analysis rules that help you avoid dead‑code elimination risks in benchmarks:

- SF1001: Unobserved result in benchmark method
- SF1002: Constant‑foldable patterns inside measured code
- SF1003: Hot loops without `Consumer.Consume(...)` usage

These analyzers will ship with configurations and code fixes. Track progress in the Phase 2 Quick Start and release notes.
