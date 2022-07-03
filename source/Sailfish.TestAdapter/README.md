# Sailfish Test Adapter - *A work in progress*

This project contains the minimal necessary code required to hook into the VS Test framework.

For extensive documentation on the VS Test Framework (for the purposes of understanding how to write minimal adapters) you can
visite [these docs](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0004-Adapter-Extensibility.md).

For a video overview of the test framework and how it hangs together, you can view this
this [On .NET episode](https://docs.microsoft.com/en-us/shows/on-net/exploring-the-visual-studio-test-platform#time=22m24s).

These two resources will provide most, if not all, of what you need to understand this adapter and how to write your own.

## How does the Sailfish adapter work?

This adapter provides the logic to discover classes annotated with the `SailfishAttribute`, and then execute the standard execution method decorated with the the `ExecutePerformanceCheckAttribute`.
One tests are discovered, they are provided to the `SailfishExecutor`, which accepts an optional callback. When each method test case is completed, the callback will produce a TestResult object that
is then delivered to the `frameworkHandle`. This handle provides feedback to the test framework so that your IDE is updated with the result of the test.

## WIP Debugging

Currently, this is a massive pain in the bum.

I've got:

- logging set up to write to a file (serilog goes silent when it encounters a type exception or whatever).
- A copy of the vstest.console.exe (2019) and all its required dlls copied into a directory and a command that looks like this:

```
.\TestPlatform\vstest.console.exe "~\Sailfish\source\UsingTheIDE\bin\Debug\net6.0\UsingTheIDE.dll" /TestAdapterPath:"~\Sailfish\source\UsingTheIDE\bin\Debug\net6.0"
```

(replace ~ with the full path to your files)

Running this will cause logs to deposit into whatever file you set in: CustomLogging.cs

- This is purely a temporary hack to get me around errors being swallowed.
  The name `logger` was used becuase I didn't want to go back and change every usage of 'logger' from serilog.
- I just implemented myown `.Verbose` method that behaves essentially the same way as serilogs structured template.