using System;
using System.Collections.Generic;
using Sailfish.Statistics;

namespace Sailfish.Execution
{
    internal class TestExecutionResult
    {
        public TestExecutionResult(TestInstanceContainer container, List<string> messages)
        {
            Exception = null;
            IsSuccess = true;
            StatusCode = StatusCode.Success;

            TestInstanceContainer = container;
            Messages = messages;
            ExecutionSettings = container.ExecutionSettings;
            PerformanceTimerResults = container.Invocation.GetPerformanceResults();
        }

        public TestExecutionResult(TestInstanceContainer container, Exception exception)
        {
            Exception = exception;
            IsSuccess = false;
            StatusCode = StatusCode.Failure;

            TestInstanceContainer = container;
            Messages = null;
            ExecutionSettings = container.ExecutionSettings;
            PerformanceTimerResults = container.Invocation.GetPerformanceResults(false);
        }

        public ExecutionSettings? ExecutionSettings { get; }
        public List<string>? Messages { get; }
        public Exception? Exception { get; }
        public StatusCode StatusCode { get; }
        public bool IsSuccess { get; }
        public TestInstanceContainer TestInstanceContainer { get; set; }
        public PerformanceTimer PerformanceTimerResults { get; }
    }
}