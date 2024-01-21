using MediatR;
using Sailfish.Execution;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Contracts.Private.ExecutionCallbackHandlers;
internal class ExecutionCompletedNotification(TestCaseExecutionResult testCaseExecutionResult, TestInstanceContainer testInstanceContainer, IEnumerable<dynamic> testCaseGroup) : INotification
{
    public TestCaseExecutionResult TestCaseExecutionResult { get; set; } = testCaseExecutionResult;
    public TestInstanceContainer TestInstanceContainer { get; set; } = testInstanceContainer;
    public IEnumerable<dynamic> TestCaseGroup { get; set; } = testCaseGroup;
}
