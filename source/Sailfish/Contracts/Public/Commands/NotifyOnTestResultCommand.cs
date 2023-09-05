using System;

using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Commands;

public class NotifyOnTestResultCommand : INotification
{
    public NotifyOnTestResultCommand(
        TestResultFormats testResultFormats,
        SailDiffSettings sailDiffSettings,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        TestResultFormats = testResultFormats;
        SailDiffSettings = sailDiffSettings;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
    }

    public TestResultFormats TestResultFormats { get; }
    public SailDiffSettings SailDiffSettings { get; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
}