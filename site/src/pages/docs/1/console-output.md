---
title: Console Output
---

## Overview

When you run Sailfish via `dotnet test` or from a console app that calls `SailfishRunner`, Sailfish writes human-readable logs to **standard output** using its logging abstraction.

This output is intended to be:

- Easy to skim in a terminal
- Safe to pipe into log aggregators
- Controlled via the same `LogLevel` settings used everywhere else

## Default console logger

By default Sailfish uses an internal `DefaultLogger` that writes messages like:

```text
[10:32:10 INF] Starting Sailfish run
[10:32:11 INF] Completed 3 tests in 00:00:01.234
[10:32:11 WRN] Environment health score is low (jitter detected)
```

Key properties:

- Each line starts with a timestamp in `HH:mm:ss` format.
- A short log-level tag is printed in colour (`VRB`, `DBG`, `INF`, `WRN`, `ERR`, `FATAL`).
- Message templates support `{placeholders}` which are filled with the extra values you pass.
- Messages below the configured minimum log level are suppressed.

The supported levels are:

- `Verbose`
- `Debug`
- `Information`
- `Warning`
- `Error`
- `Fatal`

## Controlling verbosity

Use `RunSettingsBuilder` to set the minimum level for a run:

```csharp
var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithMinimumLogLevel(LogLevel.Information)
    .Build();

await SailfishRunner.Run(settings);
```

With `Information` as the minimum, verbose and debug messages are dropped, while warnings and errors are always shown.

## Using your own logger

You can plug in your own implementation of `Sailfish.Logging.ILogger` and route Sailfish logs into Serilog, NLog, Application Insights, etc.:

```csharp
var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithCustomLogger(new MySerilogAdapter(logger))
    .Build();
```

Any component that takes an `ILogger` (including the test adapter formatters) will use your implementation.

## Silencing console output

To completely silence console logging for a run while still producing tracking files and markdown/CSV outputs, call:

```csharp
var settings = RunSettingsBuilder
    .CreateBuilder()
    .DisableLogging()
    .Build();
```

This swaps the global logger to a `SilentLogger` and marks logging as disabled in the run settings.

