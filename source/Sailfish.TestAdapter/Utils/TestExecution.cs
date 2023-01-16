using System;
using System.Collections.Generic;
using System.Threading;
using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Execution;
using Serilog;

namespace Sailfish.TestAdapter.Utils;


internal class TestExecution
{
    public static void ExecuteTests(List<TestCase> testCases, IFrameworkHandle frameworkHandle)
    {
        CustomLogger.VerbosePadded("We are calling the Execute Tests method");

        if (testCases.Count == 0)
        {
            CustomLogger.VerbosePadded("No tests were discovered in this thang");
            return;
        }

        foreach (var testCase in testCases)
        {
            var testTypeProperty = TestProperty.Find(TestCaseItemCreator.TestCaseId)!;
            var testTypeObj = testCase.GetPropertyValue(testTypeProperty);
            if (testTypeObj is null) throw new Exception("OMG TestTypeProp not being set!?");

            var testType = (Type)testTypeObj;

            var executor = new SailFishTestExecutor(
                Log.Logger,
                new TestInstanceContainerCreator(
                    new DllTypeResolver(testCase.Source),
                    new ParameterGridCreator(
                        new ParameterCombinator(),
                        new IterationVariableRetriever())),
                new TestCaseIterator());


            void TestResultCallback(TestExecutionResult result)
            {
                var testResult = new TestResult(tc);

                if (result.Exception is not null)
                {
                    testResult.ErrorMessage = result.Exception.Message;
                    testResult.ErrorStackTrace = result.Exception.StackTrace;
                }

                testResult.Outcome = result.StatusCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;
                testResult.DisplayName = tc.DisplayName;

                testResult.StartTime = result.PerformanceTimerResults.GlobalStart;
                testResult.EndTime = result.PerformanceTimerResults.GlobalStop;
                testResult.Duration = result.PerformanceTimerResults.GlobalDuration;
                CustomLogger.Verbose("Test Executed -- recording and sending the result to the framework handle - {TestResult}", testResult.ToString());
                frameworkHandle.RecordResult(testResult);
            }


            var result = executor.Execute(testType, TestResultCallback, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}