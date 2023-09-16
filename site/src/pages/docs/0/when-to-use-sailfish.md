---
title: When should I use Sailfish?
---

Benchmarking software performance is kind of like measuring the size of objects in the universe. Sometimes you need to measure very small things, like atoms, and other times you'll need to measure very large things, like stars.

The same is true for benchmarking. Sometimes you need to measure extremely quick things - like an addtion operation that completes in nanoseconds. Other times you'll be measuring relatively slow things, like an API request that return in over milliseconds.

When you need a library that can take measurements not intended for scientific journals - Sailfish is the tool to reach for.

**Sailfish**:

 - runs in process, so you can debug your tests without attaching to an external process

 - uses reliable tools for performing statistical analyses on your results, such as outlier detection or distribution testing

 - has a test adapter that you can install to make salefish tests behave like NUnit or xUnit in the IDE