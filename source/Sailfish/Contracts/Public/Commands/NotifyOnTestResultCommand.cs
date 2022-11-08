using System;
using Accord.Collections;
using MediatR;
using Sailfish.Analysis;

namespace Sailfish.Contracts.Public.Commands;

public class NotifyOnTestResultCommand : INotification
{
    public NotifyOnTestResultCommand(
        TestResultFormats testResultFormats,
        TestSettings testSettings,
        DateTime timeStamp,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args)
    {
        TestResultFormats = testResultFormats;
        TestSettings = testSettings;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
    }

    public TestResultFormats TestResultFormats { get; }
    public TestSettings TestSettings { get; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }
}