# Interpreting Your Results

The tables that are produced by Sailfish are intended to be self-explanatory. They will contain your unique test case id (referred to in the tables as the 'display name'), various descriptive statistics about your test run, and if applicable, the statistical comparison test.

Yuo'll notice below that results are grouped first by test class, e.g. `ReadmeExample`, then by test method, then ordered by variable.

```
-----------------------------------
ReadmeExample
-----------------------------------

 | DisplayName                      | Median  | Mean         | StdDev     | Variance |
 |-----------------------------------------------------------------------------------|
 | ReadmeExample.TestMethod(N: 1)   | 109 ms  | 108.66667 ms | 3.01109 ms | 9.06667  |
 | ReadmeExample.TestMethod(N: 10)  | 1010 ms | 1011.5 ms    | 8.96103 ms | 80.3     |

-----------------------------------
WilcoxonRankSumTest results comparing:
Before: ~\tracking_directory\PerformanceTracking_2023-19-2--09-02-00.csv.tracking
After: ~\tracking_directory\PerformanceTracking_2023-19-2--09-02-51.csv.tracking
-----------------------------------
Note: The change in execution time is significant if the PValue is less than 0.005

 | DisplayName | MeanOfBefore | MeanOfAfter  | MedianOfBefore | MedianOfAfter | PValue | TestStatistic | ChangeDescription |
 |-------------------------------------------------------------------------------------------------------------------------|
 | ReadmeExample.TestMethod(N: 1)   | 108.66 ms  | 111.16 ms  | 109 ms   | 111 ms     | 0.3008  | 25   | No Change   |
 | ReadmeExample.TestMethod(N: 10)  | 1011.5 ms  | 1013.33 ms | 1010 ms  | 1013.5 ms  | 0.5541  | 22   | No Change   |


No regressions or improvements found.
Test run was valid
```

## Next: [Utilities](../10/utilities.md)
