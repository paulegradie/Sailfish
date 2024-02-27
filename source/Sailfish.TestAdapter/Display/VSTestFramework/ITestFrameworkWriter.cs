using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.Display.VSTestFramework;

internal interface ITestFrameworkWriter
{
    void RecordStart(TestCase testCase);

    void RecordResult(TestResult testResult);

    void RecordEnd(TestCase testCase, TestOutcome testOutcome);
}