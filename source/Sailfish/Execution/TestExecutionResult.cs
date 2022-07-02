using System;
using System.Collections.Generic;
using Sailfish.Statistics;

namespace Sailfish.Execution
{
    public class TestExecutionResult
    {
        private TestExecutionResult(TestInstanceContainer container, List<string> messages, bool isSuccess, Exception? exception = null)
        {
            TestInstanceContainer = container;
            Messages = messages?.ToArray()!;
            Exception = exception;
            IsSuccess = isSuccess;
            ExecutionSettings = container?.ExecutionSettings!; // Trick compiler on failure
        }

        public TestInstanceContainer TestInstanceContainer { get; set; }

        public string[] Messages { get; set; }
        public Exception? Exception { get; set; }
        public StatusCode StatusCode { get; set; }
        public PerformanceTimer PerformanceTimerResults { get; set; } = null!;
        public ExecutionSettings ExecutionSettings { get; set; }

        public bool IsSuccess { get; }


        public static TestExecutionResult CreateSuccess(TestInstanceContainer container, List<string> messages)
        {
            return new TestExecutionResult(container, messages, true)
            {
                StatusCode = StatusCode.Success,
                PerformanceTimerResults = container.Invocation.GetPerformanceResults()
            };
        }

        public static TestExecutionResult CreateFailure(TestInstanceContainer? container, List<string>? messages, Exception exception)
        {
            return new TestExecutionResult(container!, messages!, false, exception)
            {
                StatusCode = StatusCode.Failure,
                PerformanceTimerResults = container?.Invocation?.GetPerformanceResults()! // Trick the compiler on failure
            };
        }
    }
}