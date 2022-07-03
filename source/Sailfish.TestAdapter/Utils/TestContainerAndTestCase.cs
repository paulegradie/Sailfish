using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Execution;

namespace Sailfish.TestAdapter.Utils;

internal class TestContainerAndTestCase
{
    public TestContainerAndTestCase(TestCase testCase, TestInstanceContainer testInstanceContainer)
    {
        TestCase = testCase;
        TestInstanceContainer = testInstanceContainer;
    }

    public TestCase TestCase { get; }
    public TestInstanceContainer TestInstanceContainer { get; }
}