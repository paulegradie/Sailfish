---
title: Which Test To Use
---

When customizing the TestSettings **TestType** (either via .sailfish.json or RunSettingsBuilder), you have three options to choose from.

You can follow this rule of thumb when choosing:

```python
if (your test makes requests over a network):
    one of:
    - TwoSampleWilcoxonSignedRankTest
    - WilcoxonRankSumTest
    - KolmogorovSmirnovTest
else:
    - TTest
