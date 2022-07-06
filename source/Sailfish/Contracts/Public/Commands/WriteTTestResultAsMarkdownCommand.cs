using System;
using MediatR;
using Sailfish.Presentation.TTest;

namespace Sailfish.Contracts.Public.Commands;

public class WriteTTestResultAsMarkdownCommand : INotification
{
    public WriteTTestResultAsMarkdownCommand(string content, string outputDirectory, TTestSettings testSettings, DateTime timeStamp)
    {
        Content = content;
        OutputDirectory = outputDirectory;
        TestSettings = testSettings;
        TimeStamp = timeStamp;
    }

    public string Content { get; set; }
    public string OutputDirectory { get; set; }
    public DateTime TimeStamp { get; }
    public TTestSettings TestSettings { get; set; }
}