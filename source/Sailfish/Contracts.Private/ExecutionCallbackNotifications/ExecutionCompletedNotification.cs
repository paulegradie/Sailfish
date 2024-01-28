using MediatR;
using Sailfish.Execution;
using System.Collections.Generic;

namespace Sailfish.Contracts.Private.ExecutionCallbackNotifications;
internal class ExecutionCompletedNotification(TestCaseExecutionResult testCaseExecutionResult, TestInstanceContainer testInstanceContainer, IEnumerable<dynamic> testCaseGroup) : INotification
{
    public TestCaseExecutionResult TestCaseExecutionResult { get; set; } = testCaseExecutionResult;
    public TestInstanceContainer TestInstanceContainer { get; set; } = testInstanceContainer;
    public IEnumerable<dynamic> TestCaseGroup { get; set; } = testCaseGroup;
}
