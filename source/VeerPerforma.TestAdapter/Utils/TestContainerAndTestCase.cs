using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using VeerPerforma.Execution;

namespace VeerPerforma.TestAdapter.Utils;

public class TestContainerAndTestCase
{
    public TestContainerAndTestCase(TestCase testCase, TestInstanceContainer testInstanceContainer)
    {
        TestCase = testCase;
        TestInstanceContainer = testInstanceContainer;
    }

    public TestCase TestCase { get; }
    public TestInstanceContainer TestInstanceContainer { get; }
}