using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.TestProperties;

public static class ManagedMethodNameDefinition
{
    internal static readonly TestProperty ManagedMethod = TestProperty.Register(
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


// public static class ManagedTypeProperty
// {
//     internal static readonly TestProperty ManagedTypePropertyDefinition = TestProperty.Register(
//         id: "TestCase.ManagedType",
//         label: "ManagedType",
//         category: string.Empty,
//         description: string.Empty,
//         valueType: typeof(string),
//         validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
//         attributes: TestPropertyAttributes.Immutable,
//         owner: typeof(TestCase));
// }
//
// public static class ManagedMethodProperty
// {
//     internal static readonly TestProperty ManagedMethodPropertyDefinition = TestProperty.Register(
//         id: "TestCase.ManagedMethod",
//         label: "ManagedMethod",
//         category: string.Empty,
//         description: string.Empty,
//         valueType: typeof(string),
//         validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
//         attributes: TestPropertyAttributes.Immutable,
//         owner: typeof(TestCase));
// }