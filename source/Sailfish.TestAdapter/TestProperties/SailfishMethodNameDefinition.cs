using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.TestProperties;

public static class SailfishMethodNameDefinition
{
    public const string MethodName = "MethodName";

    internal static readonly TestProperty SailfishMethodNameDefinitionProperty = TestProperty.Register(
        $"Sailfish.{MethodName}Definition",
        $"{MethodName}Definition",
        typeof(string),
        TestPropertyAttributes.Immutable,
        typeof(TestCase)
    );
}