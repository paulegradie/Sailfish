---
title: Selecting the right Statistical Test
---

Choosing the right statistical test for your experiment is of utmost importance.

The key thing to consider when making your selection is what kind of distributions you are comparing.

When you run a sailfish test, you produce a data distribution, represented as an array of numbers. The more numbers you have, the more confident you can be that your distribution sample is truly representative of that ground truth.

When you want to compare two distributions (e.g. a before and after), then the type of test you will use will depend on which kind of distribution your data follows.

Typically, you will want to avoid using a 'T-test', since there is an underlaying assumption that the data must follow a normal distribution, and this will rarely be the case.

This is why we provide alternate tests, such as the MannWhitneyWilcoxonTest, which do not assume normally distributed data.
