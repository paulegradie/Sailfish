using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Sailfish.TestAdapter.Display.VSTestFramework;

internal class TestFrameworkWriter : ITestFrameworkWriter
{
    private readonly IFrameworkHandle _frameworkHandle;

    public TestFrameworkWriter(IFrameworkHandle frameworkHandle)
    {
        _frameworkHandle = frameworkHandle;
    }

    public void RecordStart(TestCase testCase)
    {
        _frameworkHandle.RecordStart(testCase);
    }

    public void RecordResult(TestResult testResult)
    {
        _frameworkHandle.RecordResult(testResult);
    }

    public void RecordEnd(TestCase testCase, TestOutcome testOutcome)
    {
        _frameworkHandle.RecordEnd(testCase, testOutcome);
    }
}