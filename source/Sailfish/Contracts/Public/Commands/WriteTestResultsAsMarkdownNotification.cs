using System;

using MediatR;
using Sailfish.Analysis.SailDiff;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Commands;

public class WriteTestResultsAsMarkdownNotification : INotification
{
    public WriteTestResultsAsMarkdownNotification(
        string markdownTable,
        string outputDirectory,
        SailDiffSettings sailDiffSettings,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        MarkdownTable = markdownTable;
        OutputDirectory = outputDirectory;
        SailDiffSettings = sailDiffSettings;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
    }

    public string MarkdownTable { get; }
    public string OutputDirectory { get; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
    public SailDiffSettings SailDiffSettings { get; }
}