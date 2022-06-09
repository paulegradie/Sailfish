using System.Reflection;
using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using VeerPerforma.Attributes.TestHarness;
using VeerPerforma.Execution;
using VeerPerforma.Registration;
using VeerPerforma.TestAdapter.ExtensionMethods;
using VeerPerforma.Utils;

namespace VeerPerforma.TestAdapter;

[ExtensionUri(ExecutorUriString)]
public class TestExecutor : ITestExecutor
{
    public const string ExecutorUriString = "executor://veerperformaexecutor/v1";
    public static readonly Uri ExecutorUri = new(TestExecutor.ExecutorUriString);

    public bool Cancelled = false;

    public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
        var testCases = sources.DiscoverTests(); // veer performa test cases are the class. Internal execution logic will handle calling methods.
        RunTests(testCases, runContext, frameworkHandle);
    }

    public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
        var testCases = tests.ToArray();
        // test cases are pointing at a class, which is only allowed to 1 execution method.
        // This iteration is not finding methods in the adapter to keep iteration 1 simple as a POC.

        if (testCases.Length == 0) return;
        var referenceCase = testCases[0];

        var assembly = Assembly.LoadFile(referenceCase.Source);
        AppDomain.CurrentDomain.Load(assembly.GetName());

        var perfTestTypes = assembly
            .GetTypes()
            .Where(x => x.HasAttribute<VeerPerformaAttribute>())
            .ToArray();

        var paramGridCreator = new ParameterGridCreator(new ParameterCombinator());
        var executor = CompositionRoot().Resolve<IVeerTestExecutor>();

        var displayNameToTypeMap = MapTypesToTheirTestCaseArrays(perfTestTypes, paramGridCreator, testCases);
        foreach (var perfTestType in perfTestTypes)
        {
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
            result.GetAwaiter().GetResult(); // There is no async method on the interface
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

        frameworkHandle.RecordResult(testResult);
    }


    private static Dictionary<Type, TestCase[]> MapTypesToTheirTestCaseArrays(Type[] perfTestTypes, ParameterGridCreator paramGridCreator, TestCase[] testCases)
    {
        var testCaseMap = new Dictionary<Type, TestCase[]>();
        foreach (var testType in perfTestTypes)
        {
            var combos = paramGridCreator.GenerateParameterGrid(testType);
            var methodName = combos.Item1.Single();
            var typeDisplayNames =
                combos
                    .Item2
                    .Select(
                        combo =>
                            DisplayNameHelper.CreateDisplayName(
                                testType,
                                methodName,
                                DisplayNameHelper.CreateParamsDisplay(combo)));

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