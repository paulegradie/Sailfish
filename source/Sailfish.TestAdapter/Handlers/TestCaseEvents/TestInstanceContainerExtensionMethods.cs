using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Public.Models;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter.Handlers.TestCaseEvents;

internal static class TestInstanceContainerExtensionMethods
{
    public static TestCase GetTestCaseFromTestCaseGroupMatchingCurrentContainer(this TestInstanceContainerExternal container, IEnumerable<TestCase> testCaseGroup)
    {
        return testCaseGroup.Single(testCase =>
            container.TestCaseId.DisplayName.EndsWith(testCase.GetPropertyHelper(SailfishManagedProperty.SailfishDisplayNameDefinitionProperty)));
    }
}