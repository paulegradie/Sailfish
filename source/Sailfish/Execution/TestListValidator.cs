using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.ExtensionMethods;
using Serilog;

namespace Sailfish.Execution
{
    internal class TestListValidator : ITestListValidator
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
                if (!TypeHasMoreThanZeroExecutionMethods(test))
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

        private bool TestsAreRequestedButCannotFindAllOfThem(string[] testsRequestedByUser, string[] filteredTestNames, out List<string> missingTests)
        {
            missingTests = testsRequestedByUser.Except(filteredTestNames).ToList();
            return testsRequestedByUser.Length > 0 && filteredTestNames.Length != testsRequestedByUser.Length;
        }
    }
}