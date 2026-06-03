using System;
using System.IO;
using System.Text.RegularExpressions;
using Sailfish.Logging;
using Shouldly;
using Xunit;

namespace Tests.Library.Logging;

// These tests redirect System.Console, which is process-global — keep them off the parallel path.
[CollectionDefinition("ConsoleCaptureSerial", DisableParallelization = true)]
public class ConsoleCaptureSerialCollection;

[Collection("ConsoleCaptureSerial")]
public class DefaultLoggerTests
{
    private static Exception MakeException(string message)
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (Exception e)
        {
            return e; // carries a real stack trace
        }
    }

    private static string Capture(Action action)
    {
        var original = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(original);
        }

        return sw.ToString();
    }

    [Fact]
    public void ExceptionOverload_FillsTemplatePlaceholders_AndIncludesExceptionMessage()
    {
        // #297: the exception overload used to log the raw template, so "{0}" printed verbatim and the
        // supplied arguments were dropped entirely.
        var output = Capture(() =>
        {
            var logger = new DefaultLogger(LogLevel.Verbose);
            logger.Log(LogLevel.Error, MakeException("boom-detail"), "stage {0} failed", "ALPHA");
        });

        output.ShouldContain("stage ALPHA failed");
        output.ShouldNotContain("{0}");
        output.ShouldContain("boom-detail"); // the exception message is still surfaced
    }

    [Fact]
    public void RepeatedIdenticalException_LogsTemplateOnce_ThenCollapsesRepeats()
    {
        // #297: one root failure surfacing repeatedly (e.g. once per test case) should not print N identical
        // full stacks — log it once, then collapse repeats to a terse counted note.
        var ex = MakeException("repeated-boom");
        var output = Capture(() =>
        {
            var logger = new DefaultLogger(LogLevel.Verbose);
            logger.Log(LogLevel.Error, ex, "the same failure");
            logger.Log(LogLevel.Error, ex, "the same failure");
            logger.Log(LogLevel.Error, ex, "the same failure");
        });

        // The full template line is logged exactly once, not three times.
        Regex.Matches(output, "the same failure").Count.ShouldBe(1);
        // The 2nd and 3rd identical occurrences collapse to a counted note.
        output.ShouldContain("occurrence #2");
        output.ShouldContain("occurrence #3");
    }
}
