---
title: When should I use Sailfish?
---

Benchmarking software performance is kind of like measuring the size of objects in the universe. Sometimes you need to measure very quick things (or small things like atoms), and other times you'll need to measure very slow things (or big things, like stars).

The same can be said for benchmarking. Sometimes you need to measure extremely quick things — like an addition operation that completes in nanoseconds. Other times you'll be measuring relatively slow things, like an API request that returns in milliseconds.

**Sailfish is the tool to reach for** when you need a library that can:

 - measure execution time at the millisecond scale
 - be worked with like a test project
 - be run in a production environment
 - measure and estimate execution complexity


**Sailfish**:

 - **runs in process**, so you can debug your tests without attaching to an external process

 - **has a test adapter** that you can install to make Sailfish tests behave like NUnit or xUnit in the IDE

 - **performs statistical analysis and predictive modelling**, leveraging outlier detection and distribution testing to estimate complexity


{% callout title="Tip: Adaptive Sampling" type="note" %}
Use [Adaptive Sampling](/docs/1/adaptive-sampling) to achieve consistent precision while minimizing runtime, especially in CI.
{% /callout %}

## How does it compare to BenchmarkDotNet?

We measured, rather than argue. The same three workloads — an EF Core query, SHA-256 hashing, and a nanosecond-scale operation — were run through both tools at 10,000 samples per series and plotted as violin distributions. The evidence behind the guidance on this page:

- **Accuracy is equivalent.** Measured like-for-like (one sample per invocation, n = 10,000), the two engines' medians land 0.5% apart on a stationary CPU workload, each with a ±0.17% confidence interval.
- **Sailfish natively answers the request-scoped question.** For an EF Core query, BenchmarkDotNet's default hot-loop mean was 51 µs while the per-call distribution had a median of 76 µs and a p99 of 326 µs. To be plain: stock BenchmarkDotNet never times individual calls — it batched this query 4,096 invocations per measurement and recorded batch averages, so a real per-call p95/p99 cannot be computed from its output. Per-call timing must be explicitly configured in BDN (and it warns when you do); it is Sailfish's default.
- **The statistics cost a fraction of the time.** The same 30,000 samples: Sailfish 34 s, BenchmarkDotNet 126 s — and at stock defaults, 3.8 s vs 71 s.
- **BenchmarkDotNet remains the right tool below ~1 µs**, where batched invocation is the only low-variance way to resolve nanosecond costs.

See the full analysis — plots, statistics, methodology, and a reproducible harness: [Sailfish vs BenchmarkDotNet](/docs/0/sailfish-vs-benchmarkdotnet).
