using System;
using System.Threading.Tasks;
using VeerPerforma.Execution;

namespace VeerPerforma
{
    /// <summary>
    ///     This class can be used in a project that is intended to define a bunch of perf tests
    ///     For example, create a project, and execute this run command. That will allow you to
    /// </summary>
    public class VeerPerformaExecutor
    {
        private readonly ITestCollector testCollector;
        private readonly ITestFilter testFilter;
        private readonly IVeerTestExecutor veerTestExecutor;

        public VeerPerformaExecutor(
            IVeerTestExecutor veerTestExecutor,
            ITestCollector testCollector,
            ITestFilter testFilter
        )
        {
            this.veerTestExecutor = veerTestExecutor;
            this.testCollector = testCollector;
            this.testFilter = testFilter;
        }

        public async Task<int> Run(string[] testNames, params Type[] testLocationTypes)
        {
            var testRun = CollectTests(testNames, testLocationTypes);
            if (testRun.IsValid)
            {
                return await veerTestExecutor.Execute(testRun.Tests);
            }

            Console.WriteLine("\r----------- Error ------------\r");
            foreach (var (reason, names) in testRun.Errors)
            {
                Console.WriteLine(reason);
                foreach (var testName in names) Console.WriteLine($"--- {testName}");
            }

            return 0;
        }

        public TestValidationResult CollectTests(string[] testNames, params Type[] locationTypes)
        {
            var perfTests = testCollector.CollectTestTypes(locationTypes);
            return testFilter.FilterAndValidate(perfTests, testNames);
        }
    }
}