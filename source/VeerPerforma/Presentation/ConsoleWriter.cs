using System;
using System.Collections.Generic;
using VeerPerforma.Statistics;
using VeerPerforma.Utils;

namespace VeerPerforma.Presentation;

public class ConsoleWriter : IConsoleWriter
{
    public void Present(CompiledResultContainer result)
    {
        PrintHeader(result.Type.Name);
        PrintResults(result.CompiledResults);
        PrintExceptions(result.Exceptions);
    }

    private void PrintHeader(string typeName)
    {
        Console.WriteLine();
        Console.WriteLine("\r-----------------------------------");
        Console.WriteLine($"\r{typeName}\r");
        Console.WriteLine("-----------------------------------\r");
    }

    private void PrintResults(List<CompiledResult> compiledResults)
    {
        var table = compiledResults.ToStringTable(
            u => u.DisplayName,
            u => u.TestCaseStatistics.Median,
            u => u.TestCaseStatistics.Mean,
            u => u.TestCaseStatistics.StdDev,
            u => u.TestCaseStatistics.Variance,
            u => u.TestCaseStatistics.InterQuartileMedian
        );

        Console.WriteLine();
        Console.WriteLine(table);
        Console.WriteLine();
    }

    private void PrintExceptions(List<Exception> exceptions)
    {
        if (exceptions.Count > 0)
            Console.WriteLine($" ---- One or more Exceptions encountered ---- ");
        foreach (var exception in exceptions)
        {
            Console.WriteLine($"Exception: {exception.Message}\r");
            if (exception.StackTrace is not null)
            {
                Console.WriteLine($"StackTrace:\r{exception.StackTrace}\r");
            }
        }
    }
}