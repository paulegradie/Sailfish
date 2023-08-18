using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.TestProperties;

public static class SailfishFormedVariableSectionDefinition
{
    internal static readonly TestProperty SailfishFormedVariableSectionDefinitionProperty = TestProperty.Register(
        id: $"Sailfish.FormedVariableSectionDefinition",
        label: $"FormedVariableSectionDefinition",
        category: string.Empty,
        description: string.Empty,
        valueType: typeof(string),
        validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
        attributes: TestPropertyAttributes.Hidden,
        owner: typeof(TestCase)
    );
}