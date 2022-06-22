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

-----------------------------------
T-Test results comparing:
Before: PerformanceResults_2022-23-6--08-36-26.cvs.tracking
After: PerformanceResults_2022-23-6--08-34-58.cvs.tracking
-----------------------------------

 | TestName                                                     | MeanOfBefore | MeanOfAfter | PValue | DegreesOfFreedom | TStatistic | ChangeDescription |
 |--------------------------------------------------------------------------------------------------------------------------------------------------------|
 | DemoPerfTest.WaitPeriodPerfTest(NTries: 2, WaitPeriod: 300)  | 311 ms       | 309.5 ms    | 0.55   | 5.714            | 0.635      | No change         |
 | DemoPerfTest.WaitPeriodPerfTest(NTries: 2, WaitPeriod: 200)  | 208.75 ms    | 10.75 ms    | 0.006  | 6                | 0          | Improved          |
 | DemoPerfTest.WaitPeriodPerfTest(NTries: 1, WaitPeriod: 300)  | 308.5 ms     | 309.75 ms   | 0.613  | 4.656            | -0.542     | No change         |
 | DemoPerfTest.WaitPeriodPerfTest(NTries: 1, WaitPeriod: 200)  | 225.5 ms     | 531.75 ms   | 0.047  | 5.917            | -0.202     | Regressed         |
 | DemoPerfTest.Other(NTries: 2, WaitPeriod: 300)               | 13.75 ms     | 14 ms       | 0.638  | 3                | -0.522     | No change         |
 | DemoPerfTest.Other(NTries: 2, WaitPeriod: 200)               | 14 ms        | 13.5 ms     | 0.36   | 5.4              | 1          | No change         |
 | DemoPerfTest.Other(NTries: 1, WaitPeriod: 300)               | 13.75 ms     | 13 ms       | 0.362  | 5.146            | 1          | No change         |
 | DemoPerfTest.Other(NTries: 1, WaitPeriod: 200)               | 12.5 ms      | 14.5 ms     | 0.378  | 3.688            | -1         | No change         |

