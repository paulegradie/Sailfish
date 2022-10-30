using System;
using System.Collections.Generic;
using Accord.Collections;
using MediatR;
using Sailfish.Presentation.TTest;

namespace Sailfish.Contracts.Public.Commands;

public class WriteTTestResultAsCsvCommand : INotification
{
    public readonly DateTime TimeStamp;
    public List<NamedTTestResult> CsvRows { get; }
    public string OutputDirectory { get; }
    public TestSettings TestSettings { get; }
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }

    public WriteTTestResultAsCsvCommand(
        List<NamedTTestResult> csvRows,
        string outputDirectory,
        TestSettings testSettings,
        DateTime timeStamp,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args)
    {
        TimeStamp = timeStamp;
        CsvRows = csvRows;
        OutputDirectory = outputDirectory;
        TestSettings = testSettings;
        Tags = tags;
        Args = args;
    }
}