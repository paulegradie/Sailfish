using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.TestProperties;

public static class SailfishMethodNameDefinition
{
    internal static readonly TestProperty SailfishMethodName = TestProperty.Register(
        id: "TestCase.ManagedMethod",
        label: "ManagedMethod",
        category: string.Empty,
        description: string.Empty,
        valueType: typeof(string),
        validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
        attributes: TestPropertyAttributes.Immutable,
        owner: typeof(TestCase)
    );
}