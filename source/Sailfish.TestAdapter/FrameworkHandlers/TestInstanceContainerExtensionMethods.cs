using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Execution;
using Sailfish.TestAdapter.TestProperties;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.TestAdapter.FrameworkHandlers;
internal static class TestInstanceContainerExtensionMethods
{
    public static TestCase GetTestCaseFromTestCaseGroupMatchingCurrentContainer(this TestInstanceContainer container, IEnumerable<TestCase> testCaseGroup)
    {
        return testCaseGroup.Single(testCase =>
            container.TestCaseId.DisplayName.EndsWith(testCase.GetPropertyHelper(SailfishManagedProperty.SailfishDisplayNameDefinitionProperty)));
    }
}
