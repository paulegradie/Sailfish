---
title: When should I use Sailfish?
---

Benchmarking software performance is kind of like measuring the size of objects in the universe. Sometimes you're going to measure very small things, like atoms, and other times you're going to measure very large things, like stars.

The same is true for benchmarking. Sometimes you are going measure extremely quick things - like a string concatenation operation that completese in **under 1000 nanoseconds**. Other times you'll be measuring relatively slow things, like an API request that returns in **over 10 milliseconds**.

When you need results where the number of significant digits need not be a limiting factor of your analysis - Sailfish is the tool to reach for.

**Sailfish**:

 - runs in process, so you can debug your tests without attaching to external processes.

 - uses reliable tools for performing staticial analyses on your results, such as outlier detection or distribution testing.

 - has a test adapter that you can install is if it were an NUnit or xUnit test (complete with play buttons).

 - can be easily integrated into a monitoring system that can track regressions