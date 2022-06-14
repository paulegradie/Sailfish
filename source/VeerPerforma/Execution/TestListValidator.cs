using Serilog;
using VeerPerforma.Attributes;
using VeerPerforma.Utils;

namespace VeerPerforma.Execution;

public class TestListValidator : ITestListValidator
{
    private readonly ILogger logger;

    public TestListValidator(ILogger logger)
    {
        this.logger = logger;
    }

    public TestValidationResult ValidateTests(string[] testsRequestedByUser, Type[] filteredTestNames)
    {
        var erroredTests = new Dictionary<string, List<string>>();
        if (TestsAreRequestedButCannotFindAllOfThem(testsRequestedByUser, filteredTestNames.Select(x => x.Name).ToArray(), out var missingTests))
        {
            logger.Fatal("Could not find the tests specified: {Tests}", testsRequestedByUser.Where(x => !filteredTestNames.Select(x => x.Name).Contains(x)));
            erroredTests.Add("Could not find the following tests:", missingTests);
        }

        if (AnyTestHaveTooManyExecutionMethods(filteredTestNames, out var invalidTests))
        {
            erroredTests.Add("The following tests have too many execution methods:", invalidTests);
        }

        if (AnyTestHasNoExecutionMethods(filteredTestNames, out var noExecutionMethodTests))
        {
            erroredTests.Add("The following tests have no execution method defined:", noExecutionMethodTests);
        }

        return erroredTests.Keys.Count > 0 ? TestValidationResult.CreateFailure(filteredTestNames, erroredTests) : TestValidationResult.CreateSuccess(filteredTestNames);
    }

    private bool AnyTestHasNoExecutionMethods(Type[] testClasses, out List<string> missingExecutionMethod)
    {
        missingExecutionMethod = new List<string>();
        foreach (var test in testClasses)
        {
            if (!TypeHasNoExecutionMethod(test))
            {
                missingExecutionMethod.Add(test.Name);
            }
        }

        return missingExecutionMethod.Count > 0;
    }

    private bool AnyTestHaveTooManyExecutionMethods(Type[] testClasses, out List<string> invalidlyStructuredTests)
    {
        invalidlyStructuredTests = new List<string>();
        foreach (var test in testClasses)
        {
            if (!TypeHasMoreThanOneExecutionMethod(test))
            {
                invalidlyStructuredTests.Add(test.Name);
            }
        }

        return invalidlyStructuredTests.Count > 0;
    }

    private static bool TypeHasMoreThanOneExecutionMethod(Type type)
    {
        return type
            .GetMethodsWithAttribute<ExecutePerformanceCheckAttribute>()
            .ToArray()
            .Length > 1;
    }

    private static bool TypeHasNoExecutionMethod(Type type)
    {
        return type
            .GetMethodsWithAttribute<ExecutePerformanceCheckAttribute>()
            .ToArray()
            .Length < 1;
    }

    private bool TestsAreRequestedButCannotFindAllOfThem(string[] testsRequestedByUser, string[] filteredTestNames, out List<string> missingTests)
    {
        missingTests = testsRequestedByUser.Except(filteredTestNames).ToList();
        return testsRequestedByUser.Length > 0 && filteredTestNames.Length != testsRequestedByUser.Length;
    }
}