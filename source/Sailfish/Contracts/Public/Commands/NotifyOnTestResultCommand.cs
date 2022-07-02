using System;
using MediatR;
using Sailfish.Presentation.TTest;

namespace Sailfish.Contracts.Public.Commands;

public class NotifyOnTestResultCommand : INotification
{
    public NotifyOnTestResultCommand(string tTestContent, TTestSettings testSettings, DateTime timeStamp)
    {
        TTestContent = tTestContent;
        TestSettings = testSettings;
        TimeStamp = timeStamp;
    }

    public string TTestContent { get; }
    public TTestSettings TestSettings { get; }
    public DateTime TimeStamp { get; }
}