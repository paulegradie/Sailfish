---
title: Logger
---

## Overview

Sailfish uses a small logging abstraction so that core components and the test adapter do not depend on any particular logging library.

At the centre is the `Sailfish.Logging.ILogger` interface and the `LogLevel` enum.

## ILogger interface

```csharp
public interface ILogger
{
    void Log(LogLevel level, string template, params object[] values);
    void Log(LogLevel level, Exception ex, string template, params object[] values);
}
```

The interface deliberately mirrors common .NET logging patterns:

- `template` is a message template with `{placeholders}`.
- `values` are formatted into the template in order.
- An overload accepts an `Exception` and writes its message and stack trace.

## Log levels

`LogLevel` defines the standard levels:

- `Verbose`
- `Debug`
- `Information`
- `Warning`
- `Error`
- `Fatal`

The default logger uses these levels to decide colouring and filtering based on the minimum level configured in the run settings.

## Default and silent loggers

Sailfish ships with two built-in implementations:

- `DefaultLogger` – writes coloured log lines to `Console.Out` with timestamp and short level tag.
- `SilentLogger` – implements `ILogger` but drops all messages.

The static `Sailfish.Logging.Log.Logger` property holds the current logger and is initialised to `SilentLogger` until a run configures logging.

## Configuring logging via RunSettingsBuilder

Use `RunSettingsBuilder` to control logging for a run:

```csharp
var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithMinimumLogLevel(LogLevel.Information)   // filter noisy logs
    .WithCustomLogger(new MySerilogAdapter(logger)) // optional custom logger
    .Build();
```

Logging options:

- `.WithMinimumLogLevel(LogLevel level)` – sets the minimum level for messages to be emitted.
- `.WithCustomLogger(ILogger logger)` – supplies your own implementation; if omitted, Sailfish falls back to `DefaultLogger`.
- `.DisableLogging()` – uses `SilentLogger` and marks logging as disabled for the run.

## Relationship to console and test output

The same `ILogger` instance is passed into components that render:

- Console/text summaries for Sailfish test results
- Exception details when tests fail
- Tables for the Test Output window in the Sailfish test adapter

By plugging in a custom `ILogger`, you can redirect these messages to any destination your logging framework supports while keeping Sailfish's higher-level behaviour unchanged.

