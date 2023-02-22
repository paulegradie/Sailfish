using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.TestProperties;

public static class SailfishTestTypeFullNameDefinition
{
    public const string TestTypeFullName = "TestTypeFullName";

    internal static readonly TestProperty SailfishTestTypeFullNameDefinitionProperty = TestProperty.Register(
        $"Sailfish.{TestTypeFullName}Definition",
        $"{TestTypeFullName}Definition",
        typeof(string),
        TestPropertyAttributes.Immutable,
        typeof(TestCase)
    );
}