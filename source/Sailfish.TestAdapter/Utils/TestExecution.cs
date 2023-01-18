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

        foreach (var testCase in testCases)
        {
            var serializedTestCase = JsonConvert.SerializeObject(testCase);
            var serializedTraits = JsonConvert.SerializeObject(testCase.Traits);
            frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"serializedTestCase was: {serializedTestCase}");
            frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"serializedTraits was: {serializedTraits}");

            var testTypeTrait = testCase.Traits.Single(trait => trait.Name == TestCaseItemCreator.TestTypeFullName);
            var testTypeFullName = testTypeTrait.Value;

            var assembly = LoadAssemblyFromDll(testCase.Source);
            var testType = assembly.GetType(testTypeFullName, true, true);
            if (testType is null)
            {
                frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"Unable to fine the testType from the traits: {testCase.DisplayName}");
            }

            var typeResolve = assembly.GetTypeResolverOrNull();
            var testContainerCreator = new TestInstanceContainerCreator(
                typeResolve,
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
                frameworkHandle?.SendMessage(TestMessageLevel.Informational,
                    $"Test Executed -- recording and sending the result to the framework handle - {testResult.ToString()}");
                frameworkHandle?.RecordResult(testResult);
            }

            // each provider is a method in the instance, which yields a number of cases based on the variable combos
            var testMethods = testContainerCreator.CreateTestContainerInstanceProviders(testType);
            foreach (var testMethod in testMethods.OrderBy(x => x.Method.Name))
            {
                engine.ActivateContainer(0, 0, testMethod, TestResultCallback, CancellationToken.None).GetAwaiter().GetResult();
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