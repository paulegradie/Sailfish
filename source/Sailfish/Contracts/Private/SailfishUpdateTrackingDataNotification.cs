using System;
using MediatR;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Private;

public class SailfishUpdateTrackingDataNotification : INotification
{
    public SailfishUpdateTrackingDataNotification(ClassExecutionSummaryTrackingFormat testCaseExecutionResult, DateTime timeStamp)
    {
        TestCaseExecutionResult = testCaseExecutionResult;
        DefaultFileName = DefaultFileSettings.DefaultTrackingFileName(timeStamp);
    }

    public string DefaultFileName { get; }
    public ClassExecutionSummaryTrackingFormat TestCaseExecutionResult { get; }
}