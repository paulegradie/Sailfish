A test case is a record that covers the iterations for a single performance test class.

- This class might have
    - multiple execution methods
    - zero or more Variables

When collecting statistics, we are interested in:

- How long does it take for a single test case to execute? e.g.
    - ExampleClass.TestMethod(VariableA: 2, VariableB: 5)

        - Mean
        - Median
        - StdDev
        - Interquartile Median

- A good structured log that can be written to stdout / console

----------------------------------------------------------------

`ExampleClass`

| TestCase                                              | Median | Mean  | StdDev | Variance | Interquartile |
|-------------------------------------------------------|-------|-------|--------|----------|---------------|
| ExampleClass.TestMethodA(VariableA: 1, VariableB: 1)  | 21.3s | 23.2s | 22.6s  | 15.3     | 22.3s         |
| ExampleClass.TestMethodA(VariableA: 1, VariableB: 2)  | 25.3s | 23.2s | 22.6s  | 15.3     | 22.3s         |
| ExampleClass.TestMethodA(VariableA: 2, VariableB: 1)  | 46.3s | 44.2s | 22.6s  | 15.3     | 22.3s         |
| ExampleClass.TestMethodA(VariableA: 2, VariableB: 2)  | 21.3s | 23.2s | 22.6s  | 15.3     | 22.3s         |
| ExampleClass.TestMethodB(VariableA: 1, VariableB: 1)  | 21.3s | 23.2s | 22.6s  | 15.3     | 22.3s         |
| ExampleClass.TestMethodB(VariableA: 1, VariableB: 2)  | 25.3s | 23.2s | 22.6s  | 15.3     | 22.3s         |
| ExampleClass.TestMethodB(VariableA: 2, VariableB: 1)  | 46.3s | 44.2s | 22.6s  | 15.3     | 22.3s         |
| ExampleClass.TestMethodB(VariableA: 2, VariableB: 2)  | 21.3s | 23.2s | 22.6s  | 15.3     | 22.3s         |

`OtherClass`

| TestCase                                            | Median | Mean  | StdDev | Variance | Interquartile |
|-----------------------------------------------------|--------|-------|--------|----------|---------------|
| OtherClass.TestMethodB(VariableA: 1, VariableB: 1)  | 21.3s  | 23.2s | 22.6s  | 15.3     | 22.3s         |
| OtherClass.TestMethodB(VariableA: 1, VariableB: 1)  | 21.3s  | 23.2s | 22.6s  | 15.3     | 22.3s         |   
| OtherClass.TestMethodB(VariableA: 1, VariableB: 1)  | 21.3s  | 23.2s | 22.6s  | 15.3     | 22.3s         |  
| OtherClass.TestMethodB(VariableA: 1, VariableB: 1)  | 21.3s  | 23.2s | 22.6s  | 15.3     | 22.3s         | 
| OtherClass.TestMethodB(VariableA: 1, VariableB: 1)  | 21.3s  | 23.2s | 22.6s  | 15.3     | 22.3s         |


Since tracking files are produced by default, on the second run you should see something like the following statistical test performed (unless this is disabled):

```
-----------------------------------
T-Test results comparing:
Before: PerformanceResults_2022-23-6--09-43-30.cvs.tracking
After: PerformanceResults_2022-23-6--08-49-02.cvs.tracking
-----------------------------------
Note: The change in execution time is significant if the PValue is less than 0.5 (This is typically going to be set to 0.01, not 0.5).

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