﻿using System.Collections.Generic;
using System.Linq;
using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Execution;
using Sailfish.Registration;

namespace Sailfish.TestAdapter.Utils;

internal class TestExecution
{
    private readonly ISailFishTestExecutor executor;
    private readonly ITestInstanceContainerCreator instanceContainerCreator;
    private readonly TypeLoader typeLoader;

    public TestExecution()
    {
        typeLoader = new TypeLoader();
        var container = CompositionRoot();
        executor = container.Resolve<ISailFishTestExecutor>();
        instanceContainerCreator = container.Resolve<ITestInstanceContainerCreator>();
    }

    public void ExecuteTests(List<TestCase> testCases, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
        CustomLogger.VerbosePadded("We are calling the Execute Tests method");

        if (testCases.Count == 0) return;

        var sourceDlls = testCases.Select(x => x.Source).Distinct();
        var perfTestTypes = typeLoader.LoadTypes(sourceDlls);

        foreach (var perfTestType in perfTestTypes)
        {
            var testMethods = instanceContainerCreator.CreateTestContainerInstanceProviders(perfTestType);
            foreach (var method in testMethods)
            {
                foreach (var container in method.ProvideNextTestInstanceContainer())
                {
                    var tc = testCases.SingleOrDefault(x => x.DisplayName == container.DisplayName); // Can we set a common Id property instead of the random GUID?
                    if (tc is null)
                    {
                        CustomLogger.Verbose("\r----FATAL ERROR ENCOUNTERED!! ----\r");
                        CustomLogger.Verbose("TestInstanceContainer: {ContainerName}", container.DisplayName);
                        CustomLogger.Verbose("\rThe following testCases were available in this instance:\r");
                        foreach (var testCase in testCases)
                        {
                            CustomLogger.Verbose("{DisplayName}, {Source}, {CodeFilePath}, {FQN}", testCase.DisplayName, testCase.Source, testCase.CodeFilePath, testCase.FullyQualifiedName);
                        }

                        continue;
                        // throw new Exception($"Somehow a test case wasn't found for {container.DisplayName}");
                    }

                    executor.Execute(container, (instanceContainer, result) => { TestResultCallback(frameworkHandle, tc, instanceContainer, result); }).Wait(); // this is async tho
                }
            }
        }
    }

    public void TestResultCallback(
        IFrameworkHandle frameworkHandle,
        TestCase testCase,
        TestInstanceContainer container,
        TestExecutionResult result
    )
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

    public static IContainer CompositionRoot()
    {
        var builder = new ContainerBuilder();
        builder.RegisterSailfishTypes();
        return builder.Build();
    }
}