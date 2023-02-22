using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Exceptions;

namespace Sailfish.TestAdapter.TestProperties;

public static class PropertyValueExtensionMethods
{
    public static string GetPropertyHelper(this TestCase testCase, TestProperty testProperty)
    {
        var value = testCase
            .GetPropertyValue(testProperty, $"Failed to return {testProperty.Id}");
        if (value is null) throw new SailfishException($"Failed to return {testProperty.Id}");
        return value;
    }
}