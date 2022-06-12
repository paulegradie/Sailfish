using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Serilog.Core;
using VeerPerforma.Attributes;
using VeerPerforma.Execution;
using VeerPerforma.Registration;
using VeerPerforma.TestAdapter.Utils;
using VeerPerforma.Utils;

namespace VeerPerforma.TestAdapter;

[ExtensionUri(ExecutorUriString)]
public class TestExecutor : ITestExecutor
{
    public const string ExecutorUriString = "executor://vpexecutorv2";
    public static readonly Uri ExecutorUri = new(ExecutorUriString);

    public bool Cancelled = false;
    private Logger Serilogger => Logging.CreateLogger($"YOUR_MOM.txt");

    public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
        Serilogger.Verbose("Hitting the first RunTests Method");
        foreach (var source in sources)
        {
            Serilogger.Verbose("A SOURCE: {Source}", source);
        }

        var testCases = sources.DiscoverTests(Serilogger); // veer performa test cases are the class. Internal execution logic will handle calling methods.

        Serilogger.Verbose("FOUND THE ");
        RunTests(testCases, runContext, frameworkHandle);
    }

    public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
        Serilogger.Verbose("Hitting the second RunTests method");
        var testCases = tests.ToArray();
        // test cases are pointing at a class, which is only allowed 1 execution method.
        // This version is not looking for multiple methods in the adapter in order to keep this version a simple POC.
        Serilogger.Verbose("How many Test Cases? {0}", testCases.Length.ToString());
        if (testCases.Length == 0) return;
        var referenceCase = testCases[0];

        Serilogger.Verbose("TRYING TO LOAD THIS: {Source}", referenceCase.Source); // source is a dll!

        // var assembly = referenceCase.
        var assembly = Assembly.LoadFile(referenceCase.Source); // source is a  dll!!!!
        AppDomain.CurrentDomain.Load(assembly.GetName());

        var perfTestTypes = assembly
            .GetTypes()
            .Where(x => x.HasAttribute<VeerPerformaAttribute>())
            .ToArray();

        var paramGridCreator = new ParameterGridCreator(new ParameterCombinator());
        var executor = CompositionRoot().Resolve<IVeerTestExecutor>();
 
        var displayNameToTypeMap = MapTypesToTheirTestCaseArrays(perfTestTypes, paramGridCreator, testCases, Serilogger);
        foreach (var (type, testCasesArray) in displayNameToTypeMap)
        {
            Serilogger.Verbose("This type {Type} has this many testCases: {Count}", type.Name, testCasesArray.Length);
        }
        
        foreach (var perfTestType in perfTestTypes)
        {
            Serilogger.Verbose("Working on this perf test: {0}", perfTestType.Name);

            var result = executor.Execute(
                perfTestType,
                (
                    testType,
                    testCaseIndex,
                    statusCode,
                    exception,
                    messages,
                    startTime,
                    endTime,
                    duration) =>
                {
                    TestResultCallback(
                        frameworkHandle,
                        displayNameToTypeMap[testType],
                        testType,
                        testCaseIndex,
                        statusCode,
                        exception,
                        messages,
                        startTime,
                        endTime,
                        duration,
                        Serilogger);
                });
            result.Wait(); // There is no async method on the interface
        }
    }

    public void TestResultCallback(
        IFrameworkHandle frameworkHandle,
        TestCase[] cases,
        Type testType,
        int caseIndex,
        int statusCode,
        Exception? exception,
        string[] messages,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        TimeSpan duration,
        Logger logger
    )
    {
        logger.Verbose("Case Index: {index}", caseIndex);
        logger.Verbose("This many cases though: {length}", cases.Length.ToString());
        foreach (var testCase in cases)
        {
            logger.Verbose("Display Name -- {display}", testCase.DisplayName);
        }
        var currentTestCase = cases[caseIndex];
        var testResult = new TestResult(currentTestCase);

        if (exception is not null)
        {
            testResult.ErrorMessage = exception.Message;
            testResult.ErrorStackTrace = exception.StackTrace;
        }

        testResult.Outcome = statusCode == 1 ? TestOutcome.Passed : TestOutcome.Failed;
        testResult.DisplayName = currentTestCase.DisplayName;

        testResult.StartTime = startTime;
        testResult.EndTime = endTime;
        testResult.Duration = duration;

        logger.Verbose("Test Executed -- recording and sending the result to the framework handle - {TestResult}", testResult.ToString());
        frameworkHandle.RecordResult(testResult);
    }

    private static Dictionary<Type, TestCase[]> MapTypesToTheirTestCaseArrays(Type[] perfTestTypes, ParameterGridCreator paramGridCreator, TestCase[] testCases, Logger logger)
    {
        var testCaseMap = new Dictionary<Type, TestCase[]>();
        foreach (var testType in perfTestTypes)
        {
            logger.Verbose("Working on: {0}", testType.Name);
            var combos = paramGridCreator.GenerateParameterGrid(testType);
            logger.Verbose("Logging the param Grid now -----");
            foreach (var item in combos.Item1)
            {
                logger.Verbose("Method Name From Grid combos: {Item}", item);
            }

            var methodName = combos.Item1.First();

            logger.Verbose("Method name: {MethodName}", methodName);
            var typeDisplayNames =
                combos
                    .Item2
                    .Select(
                        combo =>
                            DisplayNameHelper.CreateDisplayName(
                                testType,
                                methodName,
                                DisplayNameHelper.CreateParamsDisplay(combo)));

            foreach (var typeDisplayName in typeDisplayNames)
            {
                logger.Verbose("Full Display Name with combo: {Name}", typeDisplayName);
            }

            var testCasesUsingThisType = testCases.IntersectBy(typeDisplayNames, c => c.DisplayName).ToArray();
            testCaseMap.Add(testType, testCasesUsingThisType);
        }

        return testCaseMap;
    }

    public void Cancel()
    {
        Cancelled = true;
    }

    public static IContainer CompositionRoot()
    {
        var builder = new ContainerBuilder();
        builder.RegisterVeerPerformaTypes();
        return builder.Build();
    }
}