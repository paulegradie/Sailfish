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
    public TTestSettings TestSettings { get; }
    public OrderedDictionary<string, string> Tags { get; }

    public WriteTTestResultAsCsvCommand(List<NamedTTestResult> csvRows, string outputDirectory, TTestSettings testSettings, DateTime timeStamp, OrderedDictionary<string, string> tags)
    {
        TimeStamp = timeStamp;
        CsvRows = csvRows;
        OutputDirectory = outputDirectory;
        TestSettings = testSettings;
        Tags = tags;
    }

}