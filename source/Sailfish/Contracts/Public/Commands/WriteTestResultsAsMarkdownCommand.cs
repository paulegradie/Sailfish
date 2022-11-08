using System;
using Accord.Collections;
using MediatR;
using Sailfish.Analysis;

namespace Sailfish.Contracts.Public.Commands;

public class WriteTestResultsAsMarkdownCommand : INotification
{
    public WriteTestResultsAsMarkdownCommand(
        string markdownTable,
        string outputDirectory,
        TestSettings testSettings,
        DateTime timeStamp,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args)
    {
        MarkdownTable = markdownTable;
        OutputDirectory = outputDirectory;
        TestSettings = testSettings;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
    }

    public string MarkdownTable { get; set; }
    public string OutputDirectory { get; set; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }
    public TestSettings TestSettings { get; set; }
}