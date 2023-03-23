using System;
using System.Collections;
using System.Collections.Generic;
using Accord.Collections;
using MediatR;
using Sailfish.Analysis;

namespace Sailfish.Contracts.Public.Commands;

public class WriteTestResultsAsCsvCommand : INotification
{
    public readonly DateTime TimeStamp;
    public IEnumerable<TestCaseResults> CsvFormat { get; }
    public string OutputDirectory { get; }
    public TestSettings TestSettings { get; }
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }

    public WriteTestResultsAsCsvCommand(
        IEnumerable<TestCaseResults> csvFormat,
        string outputDirectory,
        TestSettings testSettings,
        DateTime timeStamp,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args)
    {
        TimeStamp = timeStamp;
        CsvFormat = csvFormat;
        OutputDirectory = outputDirectory;
        TestSettings = testSettings;
        Tags = tags;
        Args = args;
    }
}