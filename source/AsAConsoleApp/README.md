# Sailfish Console App Demo

This project provides a demo for how to use Sailfish as a console app.

## Example output

The demo test should #JustWork.

When the test executes, various outputs will be written to the output directory (set to your bin folder by default) as well as the console.

For example, you should see something similar to the following appear after the first run:

```
-----------------------------------
DemoPerfTest
-----------------------------------

 | DisplayName                                     | Median  | Mean    | StdDev   | Variance |
 |-------------------------------------------------------------------------------------------|
 | DemoPerfTest.Other(NTries: 1, WaitPeriod: 200)  | 14.5 ms | 14.5 ms | 1.291 ms | 1.667    |
 | DemoPerfTest.Other(NTries: 1, WaitPeriod: 300)  | 13 ms   | 13 ms   | 0.816 ms | 0.667    |
 | DemoPerfTest.Other(NTries: 2, WaitPeriod: 200)  | 13.5 ms | 13.5 ms | 0.577 ms | 0.333    |
 | DemoPerfTest.Other(NTries: 2, WaitPeriod: 300)  | 14 ms   | 14 ms   | 0 ms     | 0        |


 | DisplayName                                                  | Median | Mean      | StdDev    | Variance  |
 |-----------------------------------------------------------------------------------------------------------|
 | DemoPerfTest.WaitPeriodPerfTest(NTries: 1, WaitPeriod: 200)  | 215 ms | 231.75 ms | 41.169 ms | 1694.917  |
 | DemoPerfTest.WaitPeriodPerfTest(NTries: 1, WaitPeriod: 300)  | 310 ms | 309.75 ms | 2.217 ms  | 4.917     |
 | DemoPerfTest.WaitPeriodPerfTest(NTries: 2, WaitPeriod: 200)  | 209 ms | 208.75 ms | 6.131 ms  | 37.583    |
 | DemoPerfTest.WaitPeriodPerfTest(NTries: 2, WaitPeriod: 300)  | 311 ms | 309.5 ms  | 3.697 ms  | 13.667    |

```

This will by default produce a tracking file in a subdirectory to the output directory. So, on the second run, by default, you should also the following statistical test:

```
-----------------------------------
T-Test results comparing:
Before: PerformanceResults_2022-23-6--09-43-30.cvs.tracking
After: PerformanceResults_2022-23-6--08-49-02.cvs.tracking
-----------------------------------
Note: The change in execution time is significant if the PValue is less than 0.5

 | TestName                                                     | MeanOfBefore | MeanOfAfter | PValue | DegreesOfFreedom | TStatistic | ChangeDescription |
 |--------------------------------------------------------------------------------------------------------------------------------------------------------|
 | DemoPerfTest.WaitPeriodPerfTest(NTries: 2, WaitPeriod: 300)  | 313 ms       | 308.75 ms   | 0.038  | 5.99             | 2.655      | *Improved         |
 | DemoPerfTest.WaitPeriodPerfTest(NTries: 2, WaitPeriod: 200)  | 205.5 ms     | 212.25 ms   | 0.26   | 3.105            | -1.374     | *Regressed        |
 | DemoPerfTest.WaitPeriodPerfTest(NTries: 1, WaitPeriod: 300)  | 314 ms       | 306.75 ms   | 0.096  | 3.458            | 2.267      | *Improved         |
 | DemoPerfTest.WaitPeriodPerfTest(NTries: 1, WaitPeriod: 200)  | 229.75 ms    | 235 ms      | 0.894  | 5.986            | -0.139     | No change         |
 | DemoPerfTest.Other(NTries: 2, WaitPeriod: 300)               | 13.5 ms      | 13.75 ms    | 0.537  | 5.88             | -0.655     | No change         |
 | DemoPerfTest.Other(NTries: 2, WaitPeriod: 200)               | 13.75 ms     | 13.75 ms    | 1      | 5.602            | 0          | No change         |
 | DemoPerfTest.Other(NTries: 1, WaitPeriod: 300)               | 13 ms        | 13.75 ms    | 0.621  | 5.898            | -0.522     | No change         |
 | DemoPerfTest.Other(NTries: 1, WaitPeriod: 200)               | 11 ms        | 11.5 ms     | 0.885  | 5.682            | -0.151     | No change         |


```