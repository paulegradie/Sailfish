using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.TestProperties;

public static class SailfishDisplayNameDefinition
{
    internal static readonly TestProperty SailfishDisplayNameDefinitionProperty = TestProperty.Register(
        "Sailfish.DisplayNameDefinition",
        "DisplayNameDefinition",
        typeof(string),
        TestPropertyAttributes.Immutable,
        typeof(TestCase)
    );
}