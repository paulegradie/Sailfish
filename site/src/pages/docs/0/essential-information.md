---
title: Essential Information
---
- Tests are not parallelized
- Run order is deterministic
- Tests are run in-process

Sailfish applies outlier detection and overhead estimation to run results. The timed method is invoked through a compiled direct-call delegate (not reflection), and harness overhead is measured with a structurally identical idle baseline and subtracted — see [Measurement & Overhead](/docs/1/measurement-and-overhead). It does not perform any optimizations that would result in the tests needing to be run out-of-process.


## Confidence Intervals

Sailfish reports 95% and 99% confidence intervals by default for the mean runtime. See [Confidence Intervals](/docs/1/confidence-intervals) for details.


## Adaptive Sampling

Automatically stop collecting samples when results are stable using CV and CI width thresholds.
See [Adaptive Sampling](/docs/1/adaptive-sampling) for configuration and best practices.
