using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Execution;

namespace Sailfish.TestAdapter.Utils;

internal static class TestExecution
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
            var testTypeProperty = TestProperty.Find(TestCaseItemCreator.TestType)!;
            var testType = (Type)testCase.GetPropertyValue(testTypeProperty)!;
            if (testType is null) throw new Exception("OMG TestTypeProp not being set!?");


            var testContainerCreator = new TestInstanceContainerCreator(
                null,
                new ParameterGridCreator(
                    new ParameterCombinator(),
                    new IterationVariableRetriever()));
            var engine = new SailfishExecutionEngine(new TestCaseIterator());

            void TestResultCallback(TestExecutionResult result)
            {
                var testResult = new TestResult(testCase);

                if (result.Exception is not null)
                {
                    testResult.ErrorMessage = result.Exception.Message;
                    testResult.ErrorStackTrace = result.Exception.StackTrace;
                }

                testResult.Outcome = result.StatusCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;
                testResult.DisplayName = testCase.DisplayName;

                testResult.StartTime = result.PerformanceTimerResults.GlobalStart;
                testResult.EndTime = result.PerformanceTimerResults.GlobalStop;
                testResult.Duration = result.PerformanceTimerResults.GlobalDuration;
                CustomLogger.Verbose("Test Executed -- recording and sending the result to the framework handle - {TestResult}", testResult.ToString());
                frameworkHandle.RecordResult(testResult);
            }

            // each provider is a method in the instance, which yields a number of cases based on the variable combos
            var testMethods = testContainerCreator.CreateTestContainerInstanceProviders(testType);
            foreach (var testMethod in testMethods.OrderBy(x => x.Method.Name))
            {
                engine.ActivateContainer(0, 0, testMethod, TestResultCallback, CancellationToken.None).GetAwaiter().GetResult();
            }
        }
    }
}