using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Public.Notifications;

public class MethodComparisonCompletedNotification : INotification
{
    public MethodComparisonCompletedNotification(MethodComparisonResult comparisonResult, IEnumerable<dynamic> testCaseGroup)
    {
        ComparisonResult = comparisonResult ?? throw new ArgumentNullException(nameof(comparisonResult));
        TestCaseGroup = testCaseGroup ?? throw new ArgumentNullException(nameof(testCaseGroup));
    }

    public MethodComparisonResult ComparisonResult { get; }
    public IEnumerable<dynamic> TestCaseGroup { get; }
}
