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
        var erroredTests = new List<string>();
        if (TestsAreRequestedButCannotFindAllOfThem(testsRequestedByUser, filteredTestNames.Select(x => x.Name).ToArray(), out var missingTests))
        {
            logger.Fatal("Could not find the tests specified: {Tests}", testsRequestedByUser.Where(x => !filteredTestNames.Select(x => x.Name).Contains(x)));
            erroredTests.AddRange(missingTests);
        }

        if (AnyTestStructuresAreNotValid(filteredTestNames, out var invalidTests))
        {
            erroredTests.AddRange(invalidTests.Select(x => x.Name));
        }

        return erroredTests.Count > 0 ? TestValidationResult.CreateFailure(filteredTestNames, erroredTests.Distinct().ToList()) : TestValidationResult.CreateSuccess(filteredTestNames);
    }

    private bool AnyTestStructuresAreNotValid(Type[] testClasses, out List<Type> invalidlyStructuredTests)
    {
        invalidlyStructuredTests = new List<Type>();
        foreach (var test in testClasses)
        {
            if (!TypeHasOneExecutionMethod(test))
            {
                invalidlyStructuredTests.Add(test);
            }
        }

        return invalidlyStructuredTests.Count > 0;
    }

    private static bool TypeHasOneExecutionMethod(Type type)
    {
        return type
            .GetMethodsWithAttribute<ExecutePerformanceCheckAttribute>()
            .ToArray()
            .Length == 1;
    }

    private bool TestsAreRequestedButCannotFindAllOfThem(string[] testsRequestedByUser, string[] filteredTestNames, out List<string> missingTests)
    {
        missingTests = testsRequestedByUser.Except(filteredTestNames).ToList();
        return testsRequestedByUser.Length > 0 && filteredTestNames.Length != testsRequestedByUser.Length;
    }
}