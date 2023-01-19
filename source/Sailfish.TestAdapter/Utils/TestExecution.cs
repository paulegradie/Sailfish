using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Newtonsoft.Json;
using Sailfish.Execution;

namespace Sailfish.TestAdapter.Utils;

internal static class TestExecution
{
    public static void ExecuteTests(List<TestCase> testCases, IFrameworkHandle? frameworkHandle)
    {
        frameworkHandle?.SendMessage(TestMessageLevel.Informational, "We are calling the Execute Tests method");

        if (testCases.Count == 0)
        {
            frameworkHandle?.SendMessage(TestMessageLevel.Informational, "No tests were discovered in this thang");
            return;
        }

        var engine = new SailfishExecutionEngine(new TestCaseIterator());

        foreach (var testCase in testCases)
        {
            var testTypeTrait = testCase.Traits.Single(trait => trait.Name == TestCaseItemCreator.TestTypeFullName);
            var testTypeFullName = testTypeTrait.Value;

            var assembly = LoadAssemblyFromDll(testCase.Source);
            var testType = assembly.GetType(testTypeFullName, true, true);
            if (testType is null)
            {
                frameworkHandle?.SendMessage(TestMessageLevel.Error, $"Unable to find the following testType: {testTypeFullName}");
                continue;
            }

            var typeResolve = assembly.GetTypeResolverOrNull();
            var testContainerCreator = new TestInstanceContainerCreator(
                typeResolve,
                new ParameterGridCreator(
                    new ParameterCombinator(),
                    new IterationVariableRetriever()));

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

                testResult.ErrorMessage = result.Exception?.Message;

                frameworkHandle?.RecordResult(testResult);
                frameworkHandle?.RecordEnd(testCase, testResult.Outcome);
            }


            // each provider is a method in the instance, which yields a number of cases based on the variable combos
            var testMethods = testContainerCreator.CreateTestContainerInstanceProviders(testType);

            var methodIndex = 0;
            var totalMethodCount = testMethods.Count - 1;
            foreach (var testMethod in testMethods.OrderBy(x => x.Method.Name))
            {
                frameworkHandle?.RecordStart(testCase);
                engine.ActivateContainer(methodIndex, totalMethodCount, testMethod, TestResultCallback, CancellationToken.None).GetAwaiter().GetResult();
                methodIndex += 1;
            }
        }


        static Assembly LoadAssemblyFromDll(string dllPath)
        {
            var assembly = Assembly.LoadFile(dllPath);
            AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?
            return assembly;
        }
    }
}