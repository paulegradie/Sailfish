using System;
using System.Collections.Generic;

using MediatR;
using Sailfish.Analysis.SailDiff;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Commands;

public class WriteTestResultsAsCsvNotification : INotification
{
    public readonly DateTime TimeStamp;
    public IEnumerable<TestCaseResults> CsvFormat { get; }
    public string OutputDirectory { get; }
    public SailDiffSettings SailDiffSettings { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }

    public WriteTestResultsAsCsvNotification(
        IEnumerable<TestCaseResults> csvFormat,
        string outputDirectory,
        SailDiffSettings sailDiffSettings,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        TimeStamp = timeStamp;
        CsvFormat = csvFormat;
        OutputDirectory = outputDirectory;
        SailDiffSettings = sailDiffSettings;
        Tags = tags;
        Args = args;
    }
}