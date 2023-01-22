using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.TestProperties;

public class SailfishTestTypeDefinition
{
    internal static readonly TestProperty SailfishTestTypeDefinitionProperty = TestProperty.Register(
        "Sailfish.TestTypeDefinition",
        "TestTypeDefinition",
        typeof(Type),
        TestPropertyAttributes.None,
        typeof(TestCase));
}

public class SailfishDisplayNameDefinition
{
    internal static readonly TestProperty SailfishDisplayNameDefinitionProperty = TestProperty.Register(
        "Sailfish.DisplayNameDefinition",
        "DisplayNameDefinition",
        typeof(string),
        TestPropertyAttributes.Immutable,
        typeof(TestCase)
    );
}