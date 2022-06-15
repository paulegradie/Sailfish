using System;
using System.Collections.Generic;

namespace VeerPerforma.Execution
{
    public class TestExecutionResult
    {
        private TestExecutionResult(TestInstanceContainer container, List<string> messages, Exception? exception = null)
        {
            TestInstanceContainer = container;
            Messages = messages.ToArray();
            Exception = exception;
        }

        public TestInstanceContainer TestInstanceContainer { get; set; }

        public string[] Messages { get; set; }
        public Exception? Exception { get; set; }
        public int StatusCode { get; set; }
        public PerformanceTimer PerformanceTimerResults { get; set; } = null!;

        public static TestExecutionResult CreateSuccess(TestInstanceContainer container, List<string> messages)
        {
            return new TestExecutionResult(container, messages)
            {
                StatusCode = 0,
                PerformanceTimerResults = container.Invocation.GetPerformanceResults()
            };
        }

        public static TestExecutionResult CreateFailure(TestInstanceContainer container, List<string> messages, Exception exception)
        {
            return new TestExecutionResult(container, messages, exception)
            {
                StatusCode = 1,
                PerformanceTimerResults = container.Invocation.GetPerformanceResults()
            };
        }
    }
}