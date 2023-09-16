---
title: Critical Information
---

## **Tests are always run in sequence**

Sailfish does not parallelize test execution.

## **Tests run order is deterministic**

Sailfish does not currently randomize test order execution.

## **Tests are run in-process**

Sailfish applies outlier detection and overhead estimation to run results. It does not perform any optimizations that would result in the tests needing to be run out-of-process.

## **Test cases are instantiated just-in-time**

Each test case is instatiated separately and is yielded such that its dependencies are held in memory for the duration of the test - afterwhich they are disposed. The minimizes overhead produced from holding a long lived instances in memory.
