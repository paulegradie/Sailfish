using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.TestProperties;

public static class SailfishManagedProperty
{
    public static readonly TestProperty SailfishTypeProperty = TestProperty.Register(
        id: "Sailfish.TypeProperty",
        label: "TypeProperty",
        category: string.Empty,
        description: string.Empty,
        valueType: typeof(string),
        validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
        attributes: TestPropertyAttributes.Hidden,
        owner: typeof(SailfishManagedProperty));

    public static readonly TestProperty SailfishMethodFilterProperty = TestProperty.Register(
        id: "Sailfish.MethodProperty",
        label: "MethodProperty",
        category: string.Empty,
        description: string.Empty,
        valueType: typeof(string),
        validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
        attributes: TestPropertyAttributes.Hidden,
        owner: typeof(SailfishManagedProperty));

    public static readonly TestProperty SailfishFormedVariableSectionDefinitionProperty = TestProperty.Register(
        $"Sailfish.FormedVariableSectionDefinition",
        $"FormedVariableSectionDefinition",
        category: string.Empty,
        description: string.Empty,
        valueType: typeof(string),
        validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
        attributes: TestPropertyAttributes.Hidden,
        owner: typeof(SailfishManagedProperty));

    public static readonly TestProperty SailfishDisplayNameDefinitionProperty = TestProperty.Register(
        $"Sailfish.DisplayNameDefinition",
        $"DisplayNameDefinition",
        category: string.Empty,
        description: string.Empty,
        valueType: typeof(string),
        validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
        attributes: TestPropertyAttributes.Hidden,
        owner: typeof(SailfishManagedProperty));
}