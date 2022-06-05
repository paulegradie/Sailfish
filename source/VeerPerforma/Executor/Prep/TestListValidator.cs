using Serilog;
using VeerPerforma.Attributes.TestHarness;
using VeerPerforma.Utils.Discovery;

namespace VeerPerforma.Executor.Prep;

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

        return erroredTests.Count > 0 ? TestValidationResult.CreateFailure(erroredTests.Distinct().ToList()) : TestValidationResult.CreateSuccess();
    }

    private bool AnyTestStructuresAreNotValid(Type[] filteredTests, out List<Type> invalidlyStructuredTests)
    {
        invalidlyStructuredTests = new List<Type>();
        foreach (var test in filteredTests)
        {
            if (!TypeHasExecutionMethod(test))
            {
                invalidlyStructuredTests.Add(test);
            }
        }

        return invalidlyStructuredTests.Count > 0;
    }


    private static bool TypeHasExecutionMethod(Type type)
    {
        var methods = type
            .GetMethods()
            .Where(x => x.IsPublic)
            .Where(x => x.HasMethodWithAttribute<ExecutePerformanceCheckAttribute>());
        return methods.Count() > 0;
    }

    private bool TestsAreRequestedButCannotFindAllOfThem(string[] testsRequestedByUser, string[] filteredTestNames, out List<string> missingTests)
    {
        missingTests = testsRequestedByUser.Except(filteredTestNames).ToList();
        return testsRequestedByUser.Length > 0 && filteredTestNames.Length != testsRequestedByUser.Length;
    }
}