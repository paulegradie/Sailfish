using System;

using MediatR;
using Sailfish.Analysis;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Commands;

public class WriteTestResultsAsMarkdownCommand : INotification
{
    public WriteTestResultsAsMarkdownCommand(
        string markdownTable,
        string outputDirectory,
        TestSettings testSettings,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        MarkdownTable = markdownTable;
        OutputDirectory = outputDirectory;
        TestSettings = testSettings;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
    }

    public string MarkdownTable { get; }
    public string OutputDirectory { get; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
    public TestSettings TestSettings { get; }
}