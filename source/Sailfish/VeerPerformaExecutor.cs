using System;
using System.IO;
using System.Threading.Tasks;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Statistics;
using Sailfish.Statistics.StatisticalAnalysis;

namespace Sailfish
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
        private readonly ITwoTailedTTester twoTailedTTester;
        private readonly IVeerTestExecutor veerTestExecutor;

        public VeerPerformaExecutor(
            IVeerTestExecutor veerTestExecutor,
            ITestCollector testCollector,
            ITestFilter testFilter,
            ITestResultCompiler testResultCompiler,
            ITestResultPresenter testResultPresenter,
            ITwoTailedTTester twoTailedTTester
        )
        {
            this.veerTestExecutor = veerTestExecutor;
            this.testCollector = testCollector;
            this.testFilter = testFilter;
            this.testResultCompiler = testResultCompiler;
            this.testResultPresenter = testResultPresenter;
            this.twoTailedTTester = twoTailedTTester;
        }

        public async Task Run(string[] testNames, string directoryPath, bool noTrack, bool analyze, params Type[] testLocationTypes) // TODO: pass an object.
        {
            var testRun = CollectTests(testNames, testLocationTypes);
            if (testRun.IsValid)
            {
                var results = await veerTestExecutor.Execute(testRun.Tests);
                var compiledResults = testResultCompiler.CompileResults(results);
                await testResultPresenter.PresentResults(compiledResults, directoryPath, noTrack);

                if (analyze)
                {
                    var date = DateTime.Now.ToLocalTime().ToString("yyyy-dd-M--HH-mm-ss");
                    await twoTailedTTester.PresentTestResults(
                        directoryPath,
                        Path.Combine(directoryPath, $"t-test_{date}.md"));
                }
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