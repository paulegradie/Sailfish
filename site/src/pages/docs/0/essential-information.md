---
title: Essential Information
---
# - **Tests are not parallelized**

# - **Run order is deterministic**

# - **Tests are run in-process**

Sailfish applies outlier detection and overhead estimation to run results. It does not perform any optimizations that would result in the tests needing to be run out-of-process.
