using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Sailfish.TestAdapter.Display.VSTestFramework;

internal class TestFrameworkWriter : ITestFrameworkWriter
{
    private readonly IFrameworkHandle frameworkHandle;

    public TestFrameworkWriter(IFrameworkHandle frameworkHandle)
    {
        this.frameworkHandle = frameworkHandle;
    }

    public void RecordStart(TestCase testCase)
    {
        frameworkHandle.RecordStart(testCase);
    }

    public void RecordResult(TestResult testResult)
    {
        frameworkHandle.RecordResult(testResult);
    }

    public void RecordEnd(TestCase testCase, TestOutcome testOutcome)
    {
        frameworkHandle.RecordEnd(testCase, testOutcome);
    }
}