using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Presentation.TTest;

namespace Sailfish.Contracts.Public.Commands;

public class WriteTTestResultAsMarkdownCommand : INotification
{
    public WriteTTestResultAsMarkdownCommand(string content, string outputDirectory, TTestSettings testSettings, DateTime timeStamp, Dictionary<string, string> tags)
    {
        Content = content;
        OutputDirectory = outputDirectory;
        TestSettings = testSettings;
        TimeStamp = timeStamp;
        Tags = tags;
    }

    public string Content { get; set; }
    public string OutputDirectory { get; set; }
    public DateTime TimeStamp { get; }
    public Dictionary<string, string> Tags { get; }
    public TTestSettings TestSettings { get; set; }
}