# VeerPerforma Test Adapter

This project contains the minimal necessary code required to hook into the VS Test framework.

For extensive documentation on the VS Test Framework (for the purposes of understanding how to write minimal adapters) you can visite [these docs](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0004-Adapter-Extensibility.md).

For a video overview of the test framework and how it hangs together, you can view this this [On .NET episode](https://docs.microsoft.com/en-us/shows/on-net/exploring-the-visual-studio-test-platform#time=22m24s).

These two resources will provide most, if not all, of what you need to understand this adapter and how to write your own.

## How does the VeerPerforma adapter work?
This adapter provides the logic to discover classes annotated with the `VeerPerformaAttribute`, and then execute the standard execution method decorated with the the `ExecutePerformanceCheckAttribute`. One tests are discovered, they are provided to the `VeerPerformaExecutor`, which accepts an optional callback. When each method test case is completed, the callback will produce a TestResult object that is then delivered to the `frameworkHandle`. This handle provides feedback to the test framework so that your IDE is updated with the result of the test.