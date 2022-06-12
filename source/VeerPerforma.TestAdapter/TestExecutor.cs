using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
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

    public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
        var testCases = sources.DiscoverTests(); // veer performa test cases are the class. Internal execution logic will handle calling methods.
        RunTests(testCases, runContext, frameworkHandle);
    }

    public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
        logger.Verbose("Hitting the second RunTests method");
        var testCases = tests.ToArray();
        // test cases are pointing at a class, which is only allowed 1 execution method.
        // This version is not looking for multiple methods in the adapter in order to keep this version a simple POC.
        logger.Verbose("How many Test Cases? {0}", testCases.Length.ToString());
        foreach (var testCase in testCases)
        {
            logger.Verbose("TestCase Details: {0} - {1} - {2} - {3} - {4} - {5}", testCase.DisplayName, testCase.Source, testCase.LineNumber.ToString(), testCase.CodeFilePath, testCase.FullyQualifiedName, testCase.Id.ToString());
        }

        if (testCases.Length == 0) return;
        var referenceCase = testCases[0];

        logger.Verbose("TRYING TO LOAD THIS: {Source}", referenceCase.Source); // source is a dll!

        var assembly = Assembly.LoadFile(referenceCase.Source); // source is a  dll!!!!
        AppDomain.CurrentDomain.Load(assembly.GetName());

        var perfTestTypes = assembly
            .GetTypes()
            .Where(x => x.HasAttribute<VeerPerformaAttribute>())
            .ToArray();

        var paramGridCreator = new ParameterGridCreator(new ParameterCombinator());
        var executor = CompositionRoot().Resolve<IVeerTestExecutor>();

        var displayNameToTypeMap = MapTypesToTheirTestCaseArrays(perfTestTypes, paramGridCreator, testCases);
        foreach (var (type, testCasesArray) in displayNameToTypeMap)
        {
            logger.Verbose("This type {Type} has this many testCases: {Count}", type.Name, testCasesArray.Length.ToString());
        }

        foreach (var perfTestType in perfTestTypes)
        {
            logger.Verbose("Working on this perf test: {0}", perfTestType.Name);

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
                        duration);
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
        TimeSpan duration
    )
    {
        logger.Verbose("Case Index: {index}", caseIndex.ToString());
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

    private static Dictionary<Type, TestCase[]> MapTypesToTheirTestCaseArrays(Type[] perfTestTypes, ParameterGridCreator paramGridCreator, TestCase[] testCases)
    {
        logger.Verbose("\r------Mapping types to their test cases ----\r ");
        var testCaseMap = new Dictionary<Type, TestCase[]>();
        foreach (var testType in perfTestTypes)
        {
            logger.Verbose("Working on: {0}", testType.Name);
            logger.Verbose("Creating the param grid now -----");
            var combos = paramGridCreator.GenerateParameterGrid(testType);

            logger.Verbose("Logging the param Grid now -----");
            foreach (var item in combos.Item1)
            {
                logger.Verbose("Property Name From Grid combos: {Item}", item);
            }

            logger.Verbose("About to grab the method...");
            logger.Verbose("This is the damn type: {type}", testType.ToString());
            var methods = testType.GetMethods();
            logger.Verbose("Here are the methods...");
            foreach (var meth in methods)
            {
                logger.Verbose("Method found: {methodName}", meth.Name);
                var attys = meth.GetCustomAttributes().ToArray();
                logger.Verbose("Num attys found: {0}", attys.Length.ToString());
                foreach (var atty in attys)
                {
                    logger.Verbose("Attribute: {0}", atty.TypeId.ToString());
                }
            }

            logger.Verbose("\r-------\r\r");

            // TODO - currently we only support a single execution method
            var method = methods.Single(x => x.Name == "Go"); // hard coding for test with local demo project that has a test file with a method called "public void Go()"
            logger.Verbose("Method Name: {MethodName}", method.Name);

            logger.Verbose("Attempting to compile typeDisplayNames");
            var typeDisplayNames =
                combos
                    .Item2
                    .Select(
                        combo =>
                            DisplayNameHelper.CreateDisplayName(
                                testType,
                                method.Name,
                                DisplayNameHelper.CreateParamsDisplay(combo)))
                    .ToArray();

            foreach (var typeDisplayName in typeDisplayNames)
            {
                logger.Verbose("Full Display Name with combo: {Name}", typeDisplayName);
            }


            logger.Verbose("\rNow to get the test Cases using this type: {Type}", testType.Name);
            foreach (var testCase in testCases)
            {
                logger.Verbose("testCase display Name: {0}", testCase.DisplayName);
            }

            logger.Verbose("\r And these are the type display names we created---");
            foreach (var displayName in typeDisplayNames)
            {
                logger.Verbose("Type DisplayName: {0}", displayName);
            }

            var testCasesUsingThisType = testCases.IntersectBy(typeDisplayNames, c => c.DisplayName).ToArray();
            logger.Verbose("We did an intersect - so how many intersections did we actually find?: {intersect}", testCasesUsingThisType.Length.ToString());

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