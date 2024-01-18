using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.TestProperties;

public static class SailfishManagedProperty
{
    public static readonly TestProperty SailfishTypeProperty = TestProperty.Register(
        "Sailfish.TypeProperty",
        "TypeProperty",
        string.Empty,
        string.Empty,
        typeof(string),
        o => !string.IsNullOrWhiteSpace(o as string),
        TestPropertyAttributes.Hidden,
        typeof(SailfishManagedProperty));

    public static readonly TestProperty SailfishMethodFilterProperty = TestProperty.Register(
        "Sailfish.MethodProperty",
        "MethodProperty",
        string.Empty,
        string.Empty,
        typeof(string),
        o => !string.IsNullOrWhiteSpace(o as string),
        TestPropertyAttributes.Hidden,
        typeof(SailfishManagedProperty));

    public static readonly TestProperty SailfishFormedVariableSectionDefinitionProperty = TestProperty.Register(
        "Sailfish.FormedVariableSectionDefinition",
        "FormedVariableSectionDefinition",
        string.Empty,
        string.Empty,
        typeof(string),
        o => !string.IsNullOrWhiteSpace(o as string),
        TestPropertyAttributes.Hidden,
        typeof(SailfishManagedProperty));

    public static readonly TestProperty SailfishDisplayNameDefinitionProperty = TestProperty.Register(
        "Sailfish.DisplayNameDefinition",
        "DisplayNameDefinition",
        string.Empty,
        string.Empty,
        typeof(string),
        o => !string.IsNullOrWhiteSpace(o as string),
        TestPropertyAttributes.Hidden,
        typeof(SailfishManagedProperty));
}