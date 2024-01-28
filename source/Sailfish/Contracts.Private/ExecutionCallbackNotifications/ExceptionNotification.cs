using MediatR;
using Sailfish.Execution;
using System.Collections.Generic;

namespace Sailfish.Contracts.Private.ExecutionCallbackNotifications;
internal class ExceptionNotification(TestInstanceContainer testInstanceContainer, IEnumerable<dynamic> testCaseGroup) : INotification
{
    public TestInstanceContainer? TestInstanceContainer { get; set; } = testInstanceContainer;
    public IEnumerable<dynamic> TestCaseGroup { get; set; } = testCaseGroup;
}
