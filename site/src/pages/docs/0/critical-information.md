---
title: Critical Information
---

## **Tests are always run in sequence**

Sailfish does not parallelize test executions. The simple reason is that we are assessing how quickly your code executes and by parallelizing tests, the execution time would likely increase. To eliminate noise test neighbors on the machine executing the tests, only one test runs at a time.

## **Tests run order is deterministic**

Sailfish does not currently randomize test order execution.

## **Tests are run in-process**

Sailfish does not perform the optimizations necessary to achieve reliable sub-millisecond-resolution results. If you are interested in rigorous benchmarking, please consider using an alternative tool, such as BenchmarkDotNet. Sailfish was produced to remove much of the complexities and boilerplate required to write performance tests that don't need highly optimized execution.

The allows you to debug your tests directly in the IDE without the need to attach to an external process.

## **Test classes are instantiated just-in-time**

Sailfish uses enumerators to ensure that all of your test classes are not instantiated all at the same time. This is very convenient in cases where you are doing a lot of setup work in your constructors - for example if you are creating in memory server instances you wish you run tests against.
