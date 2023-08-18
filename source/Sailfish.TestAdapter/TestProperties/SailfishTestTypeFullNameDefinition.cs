using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.TestProperties;

public static class SailfishTestTypeFullNameDefinition
{
    internal static readonly TestProperty SailfishTestTypeFullNameProperty = TestProperty.Register(
        id: "TestCase.ManagedType",
        label: "ManagedType",
        category: string.Empty,
        description: string.Empty,
        valueType: typeof(string),
        validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
        attributes: TestPropertyAttributes.Immutable,
        owner: typeof(TestCase)
    );
}