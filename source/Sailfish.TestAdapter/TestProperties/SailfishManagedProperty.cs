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

    public static readonly TestProperty SailfishComparisonGroupProperty = TestProperty.Register(
        "Sailfish.ComparisonGroup",
        "ComparisonGroup",
        string.Empty,
        string.Empty,
        typeof(string),
        o => o == null || !string.IsNullOrWhiteSpace(o as string),
        TestPropertyAttributes.Hidden,
        typeof(SailfishManagedProperty));

    public static readonly TestProperty SailfishComparisonRoleProperty = TestProperty.Register(
        "Sailfish.ComparisonRole",
        "ComparisonRole",
        string.Empty,
        string.Empty,
        typeof(string),
        o => o == null || !string.IsNullOrWhiteSpace(o as string),
        TestPropertyAttributes.Hidden,
        typeof(SailfishManagedProperty));

    public static readonly TestProperty TestCaseHierarchyProperty = TestProperty.Register(
        "TestCase.Hierarchy",
        "Hierarchy",
        string.Empty,
        string.Empty,
        typeof(string[]),
        o => o is string[] arr && arr.Length == HierarchyTotalLevelCount,
        TestPropertyAttributes.Hidden,
        typeof(TestCase));

    public static readonly TestProperty TestCaseManagedTypeProperty = TestProperty.Register(
        "TestCase.ManagedType",
        "ManagedType",
        string.Empty,
        string.Empty,
        typeof(string),
        o => !string.IsNullOrWhiteSpace(o as string),
        TestPropertyAttributes.Hidden,
        typeof(TestCase));

    public static readonly TestProperty TestCaseManagedMethodProperty = TestProperty.Register(
        "TestCase.ManagedMethod",
        "ManagedMethod",
        string.Empty,
        string.Empty,
        typeof(string),
        o => !string.IsNullOrWhiteSpace(o as string),
        TestPropertyAttributes.Hidden,
        typeof(TestCase));

    public const int HierarchyContainerIndex = 0;
    public const int HierarchyNamespaceIndex = 1;
    public const int HierarchyClassIndex = 2;
    public const int HierarchyTestGroupIndex = 3;
    public const int HierarchyTotalLevelCount = 4;
}