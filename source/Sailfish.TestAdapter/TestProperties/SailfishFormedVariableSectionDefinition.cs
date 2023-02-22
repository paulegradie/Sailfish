using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.TestProperties;

public static class SailfishFormedVariableSectionDefinition
{
    public const string FormedVariableSection = "FormedVariableSection";

    internal static readonly TestProperty SailfishFormedVariableSectionDefinitionProperty = TestProperty.Register(
        $"Sailfish.{FormedVariableSection}Definition",
        $"{FormedVariableSection}Definition",
        typeof(string),
        TestPropertyAttributes.Immutable,
        typeof(TestCase)
    );
}