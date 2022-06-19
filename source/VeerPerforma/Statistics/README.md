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
