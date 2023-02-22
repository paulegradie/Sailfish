using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.TestProperties;

public static class SailfishDisplayNameDefinition
{
    public const string DisplayName = "DisplayName";

    internal static readonly TestProperty SailfishDisplayNameDefinitionProperty = TestProperty.Register(
        $"Sailfish.{DisplayName}Definition",
        $"{DisplayName}Definition",
        typeof(string),
        TestPropertyAttributes.Immutable,
        typeof(TestCase)
    );
}