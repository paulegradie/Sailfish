---
title: Critical Information
---

## **Tests are always run in sequence**

Sailfish does not parallelize test execution.

## **Tests run order is deterministic**

Sailfish does not currently randomize test order execution.

## **Tests are run in-process**

Sailfish applies outlier detection and overhead estimation to run results. It does not perform any optimizations that would result in the tests needing to be run out-of-process.
