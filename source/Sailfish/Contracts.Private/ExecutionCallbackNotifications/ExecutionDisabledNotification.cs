using MediatR;
using Sailfish.Execution;
using System.Collections.Generic;

namespace Sailfish.Contracts.Private.ExecutionCallbackNotifications;
internal class ExecutionDisabledNotification(TestInstanceContainer testInstanceContainer, IEnumerable<dynamic> testCaseGroup, bool disableTheGroup) : INotification
{
    public TestInstanceContainer TestInstanceContainer { get; set; } = testInstanceContainer;
    public IEnumerable<dynamic> TestCaseGroup { get; } = testCaseGroup;
    public bool DisableTheGroup { get; } = disableTheGroup;
}
