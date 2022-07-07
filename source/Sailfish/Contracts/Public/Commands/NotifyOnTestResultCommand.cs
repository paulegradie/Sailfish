using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Presentation.TTest;

namespace Sailfish.Contracts.Public.Commands;

public class NotifyOnTestResultCommand : INotification
{
    public NotifyOnTestResultCommand(TTestResultFormats tTestContent, TTestSettings testSettings, DateTime timeStamp, Dictionary<string, string> tags)
    {
        TTestContent = tTestContent;
        TestSettings = testSettings;
        TimeStamp = timeStamp;
        Tags = tags;
    }

    public TTestResultFormats TTestContent { get; }
    public TTestSettings TestSettings { get; }
    public DateTime TimeStamp { get; }
    public Dictionary<string, string> Tags { get; }
}