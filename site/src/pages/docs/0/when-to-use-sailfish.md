---
title: When should I use Sailfish?
---

Benchmarking software performance is kind of like measuring the size of objects in the universe. Sometimes you need to measure very quick things (or small things like atoms), and other times you'll need to measure very slow things (or big things, like stars).

The same can be said for benchmarking. Sometimes you need to measure extremely quick things - like an addtion operation that completes in nanoseconds. Other times you'll be measuring relatively slow things, like an API request that return in over milliseconds.

**Sailfish is the tool to reach for** when you need a library that can:

 - measure execution time at the millisecond scale
 - be worked with like a test project
 - be run in a production environment
 - measure and estimate execution complexity


**Sailfish**:

 - **runs in process**, so you can debug your tests without attaching to an external process

 - **has a test adapter** that you can install to make salefish tests behave like NUnit or xUnit in the IDE

 - **performs statistical analysis and predictive modelling**, leveraging outlier detection and distribution testing to estimate complexity


### Tip: Adaptive Sampling
Use [Adaptive Sampling](/docs/1/adaptive-sampling) to achieve consistent precision while minimizing runtime, especially in CI.
