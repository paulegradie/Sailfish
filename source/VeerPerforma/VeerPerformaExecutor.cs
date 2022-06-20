using System;
using System.Threading.Tasks;
using VeerPerforma.Execution;
using VeerPerforma.Presentation;
using VeerPerforma.Statistics;

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
        private readonly ITestResultCompiler testResultCompiler;
        private readonly ITestResultPresenter testResultPresenter;
        private readonly IVeerTestExecutor veerTestExecutor;

        public VeerPerformaExecutor(
            IVeerTestExecutor veerTestExecutor,
            ITestCollector testCollector,
            ITestFilter testFilter,
            ITestResultCompiler testResultCompiler,
            ITestResultPresenter testResultPresenter
        )
        {
            this.veerTestExecutor = veerTestExecutor;
            this.testCollector = testCollector;
            this.testFilter = testFilter;
            this.testResultCompiler = testResultCompiler;
            this.testResultPresenter = testResultPresenter;
        }

        public async Task Run(string[] testNames, params Type[] testLocationTypes)
        {
            var testRun = CollectTests(testNames, testLocationTypes);
            if (testRun.IsValid)
            {
                var results = await veerTestExecutor.Execute(testRun.Tests);
                var compiledResults = testResultCompiler.CompileResults(results);
                await testResultPresenter.PresentResults(compiledResults);
            }
            else
            {
                Console.WriteLine("\r----------- Error ------------\r");
                foreach (var (reason, names) in testRun.Errors)
                {
                    Console.WriteLine(reason);
                    foreach (var testName in names) Console.WriteLine($"--- {testName}");
                }

            }
        }

        public TestValidationResult CollectTests(string[] testNames, params Type[] locationTypes)
        {
            var perfTests = testCollector.CollectTestTypes(locationTypes);
            return testFilter.FilterAndValidate(perfTests, testNames);
        }
    }
}