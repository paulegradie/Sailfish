using System;

using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.Saildiff;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Commands;

public class NotifyOnTestResultCommand : INotification
{
    public NotifyOnTestResultCommand(
        TestResultFormats testResultFormats,
        TestSettings testSettings,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
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
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
}