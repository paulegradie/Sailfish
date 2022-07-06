using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Presentation.TTest;

namespace Sailfish.Contracts.Public.Commands;

public class WriteTTestResultAsCsvCommand : INotification
{
    public readonly DateTime TimeStamp;
    public List<NamedTTestResult> CsvRows { get; }
    public string OutputDirectory { get; }
    public TTestSettings TestSettings { get; }

    public WriteTTestResultAsCsvCommand(List<NamedTTestResult> csvRows, string outputDirectory, TTestSettings testSettings, DateTime timeStamp)
    {
        TimeStamp = timeStamp;
        CsvRows = csvRows;
        OutputDirectory = outputDirectory;
        TestSettings = testSettings;
    }

}