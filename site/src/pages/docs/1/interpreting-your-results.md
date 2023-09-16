---
title: Interpreting Your Results
---

The tables that are produced by Sailfish are intended to be self-explanatory. They will contain your unique test case id (referred to in the tables as the 'display name'), various descriptive statistics about your test run, and if applicable, the statistical comparison test.

You'll notice below that results are grouped first by test class, e.g. `ReadmeExample`, then by test method, then ordered by variable.

# IDE

When run in the IDE, sailfish produces the following result in the test output window:

```
ReadmeExample.TestMethod

Descriptive Statistics
----------------------
| Stat   |  Time (ms) |
| ---    | ---        |
| Mean   |   111.1442 |
| Median |   107.8113 |
| StdDev |     7.4208 |
| Min    |   105.9743 |
| Max    |   119.6471 |


Outliers Removed (0)
--------------------

Adjusted Distribution (ms)
--------------------------
119.6471, 105.9743, 107.8113
```

These are the basic descriptive statistics describing your Sailfish test run. Persisted outputs (such as markdown or csv files) will be found the output directory in the calling assembly's **/bin** folder. Those results will


# SailDiff

⚠️Image + description coming soon

# ScaleFish

⚠️Image + description coming soon