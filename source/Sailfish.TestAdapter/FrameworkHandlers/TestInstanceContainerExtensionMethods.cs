using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Execution;
using Sailfish.TestAdapter.TestProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.FrameworkHandlers;
internal static class TestInstanceContainerExtensionMethods
{
    public static TestCase GetTestCaseFromTestCaseGroupMatchingCurrentContainer(this TestInstanceContainer container, IEnumerable<TestCase> testCaseGroup)
    {
        return testCaseGroup.Single(testCase =>
            container.TestCaseId.DisplayName.EndsWith(testCase.GetPropertyHelper(SailfishManagedProperty.SailfishDisplayNameDefinitionProperty)));
    }
}
