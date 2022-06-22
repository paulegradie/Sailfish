using System;
using System.IO;
using System.Threading.Tasks;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Presentation.TTest;
using Sailfish.Statistics;
using Sailfish.Statistics.StatisticalAnalysis;

namespace Sailfish
{
    /// <summary>
    ///     This class can be used in a project that is intended to define a bunch of perf tests
    ///     For example, create a project, and execute this run command. That will allow you to
    /// </summary>
    public class SailfishExecutor
    {
        private readonly ITestCollector testCollector;
        private readonly ITestFilter testFilter;
        private readonly ITestResultCompiler testResultCompiler;
        private readonly ITestResultPresenter testResultPresenter;
        private readonly ITwoTailedTTestWriter twoTailedTTestWriter;
        private readonly ISailTestExecutor SailTestExecutor;

        public SailfishExecutor(
            ISailTestExecutor SailTestExecutor,
            ITestCollector testCollector,
            ITestFilter testFilter,
            ITestResultCompiler testResultCompiler,
            ITestResultPresenter testResultPresenter,
            ITwoTailedTTestWriter twoTailedTTestWriter
        )
        {
            this.SailTestExecutor = SailTestExecutor;
            this.testCollector = testCollector;
            this.testFilter = testFilter;
            this.testResultCompiler = testResultCompiler;
            this.testResultPresenter = testResultPresenter;
            this.twoTailedTTestWriter = twoTailedTTestWriter;
        }

        public async Task Run(string[] testNames, string directoryPath, bool noTrack, bool analyze, TTestSettings settings, params Type[] testLocationTypes) // TODO: pass an object.
        {
            var testRun = CollectTests(testNames, testLocationTypes);
            if (testRun.IsValid)
            {
                var results = await SailTestExecutor.Execute(testRun.Tests);
                var compiledResults = testResultCompiler.CompileResults(results);
                await testResultPresenter.PresentResults(compiledResults, directoryPath, noTrack);

                if (analyze)
                {
                    var date = DateTime.Now.ToLocalTime().ToString("yyyy-dd-M--HH-mm-ss");
                    await twoTailedTTestWriter.PresentTestResults(
                        directoryPath,
                        Path.Combine(directoryPath, $"t-test_{date}.md"), 
                        settings);
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