using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Extensions.Methods;
using Serilog;

namespace Sailfish.Execution;

internal class TestListValidator : ITestListValidator
{
    private readonly ILogger logger;

    public TestListValidator(ILogger logger)
    {
        this.logger = logger;
    }

    public TestInitializationResult ValidateTests(IEnumerable<string> testsRequestedByUser, IEnumerable<Type> filteredTestNames)
    {
        var erroredTests = new Dictionary<string, List<string>>();
        var requestedByUser = testsRequestedByUser.ToList();
        var testClasses = filteredTestNames.ToList();
        if (TestsAreRequestedButCannotFindAllOfThem(requestedByUser, testClasses.Select(x => x.Name).ToArray(), out var missingTests))
        {
            logger.Fatal("Could not find the tests specified: {Tests}", requestedByUser.Where(x => !testClasses.Select(x => x.Name).Contains(x)));
            erroredTests.Add("Could not find the following tests:", missingTests);
        }


        if (AnyTestHasNoExecutionMethods(testClasses, out var noExecutionMethodTests))
        {
            erroredTests.Add("The following tests have no execution method defined:", noExecutionMethodTests);
        }

        if (erroredTests.Keys.Count > 0)
        {
            return TestInitializationResult.CreateFailure(testClasses, erroredTests);
        }

        if (!testClasses.Any())
        {
            erroredTests.Add("No Tests Found", new List<string>());
            return TestInitializationResult.CreateFailure(testClasses, erroredTests);
        }

        return TestInitializationResult.CreateSuccess(testClasses);
    }

    private static bool AnyTestHasNoExecutionMethods(IEnumerable<Type> testClasses, out List<string> missingExecutionMethod)
    {
        missingExecutionMethod = new List<string>();
        foreach (var test in testClasses)
        {
            if (!TypeHasMoreThanZeroExecutionMethods(test) && !test.SailfishTypeIsDisabled())
            {
                missingExecutionMethod.Add(test.Name);
            }
        }

        return missingExecutionMethod.Count > 0;
    }

    private static bool TypeHasMoreThanZeroExecutionMethods(Type type)
    {
        return type
            .GetMethodsWithAttribute<SailfishMethodAttribute>()
            .ToArray()
            .Length > 0;
    }

    private static bool TestsAreRequestedButCannotFindAllOfThem(
        IEnumerable<string> testsRequestedByUser,
        IEnumerable<string> filteredTestNames,
        out List<string> missingTests)
    {
        var requestedByUser = testsRequestedByUser.ToList();
        var testNames = filteredTestNames.ToList();
        missingTests = requestedByUser.Except(testNames).ToList();
        return requestedByUser.Count > 0 && testNames.Count != requestedByUser.Count;
    }
}