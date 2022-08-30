using System;
using Accord.Collections;
using MediatR;
using Sailfish.Presentation.TTest;

namespace Sailfish.Contracts.Public.Commands;

public class NotifyOnTestResultCommand : INotification
{
    public NotifyOnTestResultCommand(TTestResultFormats tTestContent, TTestSettings testSettings, DateTime timeStamp, OrderedDictionary<string, string> tags, OrderedDictionary<string, string> args)
    {
        TTestContent = tTestContent;
        TestSettings = testSettings;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
    }

    public TTestResultFormats TTestContent { get; }
    public TTestSettings TestSettings { get; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }
}