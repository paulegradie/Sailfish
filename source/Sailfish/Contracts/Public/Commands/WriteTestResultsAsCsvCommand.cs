using System;
using System.Collections.Generic;

using MediatR;
using Sailfish.Analysis;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Commands;

public class WriteTestResultsAsCsvCommand : INotification
{
    public readonly DateTime TimeStamp;
    public IEnumerable<TestCaseResults> CsvFormat { get; }
    public string OutputDirectory { get; }
    public TestSettings TestSettings { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }

    public WriteTestResultsAsCsvCommand(
        IEnumerable<TestCaseResults> csvFormat,
        string outputDirectory,
        TestSettings testSettings,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        TimeStamp = timeStamp;
        CsvFormat = csvFormat;
        OutputDirectory = outputDirectory;
        TestSettings = testSettings;
        Tags = tags;
        Args = args;
    }
}