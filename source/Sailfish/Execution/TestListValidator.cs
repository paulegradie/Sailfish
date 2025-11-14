using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Extensions.Methods;
using Sailfish.Logging;

namespace Sailfish.Execution;

internal interface ITestListValidator
{
    TestInitializationResult ValidateTests(IEnumerable<string> testsRequestedByUser, IEnumerable<Type> filteredTestNames);
}

internal class TestListValidator(ILogger logger) : ITestListValidator
{
    private readonly ILogger _logger = logger;

    public TestInitializationResult ValidateTests(IEnumerable<string> testsRequestedByUser, IEnumerable<Type> filteredTestNames)
    {
        var erroredTests = new Dictionary<string, List<string>>();
        var requestedByUser = testsRequestedByUser.ToList();
        var testClasses = filteredTestNames.ToList();

        if (TestsAreRequestedButFailedToDisambiguate(requestedByUser, testClasses.Select(x => x.FullName ?? x.Name).ToArray(), out var missingTests))
        {
            _logger.Log(
                LogLevel.Fatal,
                "Failed to disambiguate the following tests: {Tests}",
                requestedByUser.Where(x => !testClasses.Select(x => x.FullName ?? x.Name).Contains(x)));
            erroredTests.Add("Failed to disambiguate the following tests:", missingTests);
        }

        if (AnyTestHasNoExecutionMethods(testClasses, out var noExecutionMethodTests))
            erroredTests.Add("The following tests have no execution method defined:", noExecutionMethodTests);

        if (erroredTests.Keys.Count > 0) return TestInitializationResult.CreateFailure(testClasses, erroredTests);

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
            if (!TypeHasMoreThanZeroExecutionMethods(test) && !test.SailfishTypeIsDisabled())
                missingExecutionMethod.Add(test.FullName ?? test.Name);

        return missingExecutionMethod.Count > 0;
    }

    private static bool TypeHasMoreThanZeroExecutionMethods(Type type)
    {
        return type
            .GetMethodsWithAttribute<SailfishMethodAttribute>()
            .ToArray()
            .Length > 0;
    }

    private static bool TestsAreRequestedButFailedToDisambiguate(
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