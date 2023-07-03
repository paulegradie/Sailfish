using Microsoft.CodeAnalysis;

namespace Sailfish.Analyzers.Utils;

public static class Descriptors
{
    public static readonly DiagnosticDescriptor PropertiesAssignedInGlobalSetupShouldBePublicDescriptor = DescriptorHelper.CreateDescriptor(
        AnalyzerGroups.EssentialAnalyzers,
        1000,
        isEnabledByDefault: true,
        title: "Properties initialized in the global setup must be public",
        description: "Properties that are assigned in the SailfishGlobalSetup must be public",
        messageFormat: "Property '{0}' must be public when assigned within a method decorated with the SailfishGlobalSetup attribute",
        severity: DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor PropertiesAssignedInGlobalSetupShouldHavePublicGettersDescriptor = DescriptorHelper.CreateDescriptor(
        AnalyzerGroups.EssentialAnalyzers,
        1001,
        isEnabledByDefault: true,
        title: "Properties assigned in the global setup must have public getters",
        description: "Properties assigned in the global setup must have public getters",
        messageFormat: "Property '{0}' must have a public getter when assigned within a method decorated with the SailfishGlobalSetup attribute",
        severity: DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor PropertiesAssignedInGlobalSetupShouldHavePublicSettersDescriptor = DescriptorHelper.CreateDescriptor(
        AnalyzerGroups.EssentialAnalyzers,
        1002,
        isEnabledByDefault: true,
        title: "Properties assigned in the global setup must have public setters",
        description: "Properties assigned in the global setup must have public setters",
        messageFormat: "Property '{0}' must have a public setter when assigned within a method decorated with the SailfishGlobalSetup attribute",
        severity: DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor SailfishVariablesShouldBePublicDescriptor = DescriptorHelper.CreateDescriptor(
        AnalyzerGroups.EssentialAnalyzers,
        1010,
        isEnabledByDefault: true,
        title: "Properties decorated with the SailfishVariableAttribute must be public",
        description: "Property '{0}' must be public",
        severity: DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor SailfishVariablesShouldHavePublicGettersDescriptor = DescriptorHelper.CreateDescriptor(
        AnalyzerGroups.EssentialAnalyzers,
        1011,
        isEnabledByDefault: true,
        title: "Properties decorated with the SailfishVariableAttribute must have public getters",
        description: "Property '{0}' getter must be public",
        severity: DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor SailfishVariablesShouldHavePublicSettersDescriptor = DescriptorHelper.CreateDescriptor(
        AnalyzerGroups.EssentialAnalyzers,
        1012,
        isEnabledByDefault: true,
        title: "Properties decorated with the SailfishVariableAttribute must have public setters",
        description: "Property '{0}' setter must be public",
        severity: DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor LifecycleMethodsShouldBePublic = DescriptorHelper.CreateDescriptor(
        AnalyzerGroups.EssentialAnalyzers,
        1020,
        isEnabledByDefault: true,
        title: "Sailfish lifecycle methods must be public",
        description: "Method '{0}' must be public",
        severity: DiagnosticSeverity.Error);    
    
    public static readonly DiagnosticDescriptor OnlyOneLifecycleAttributePerMethod = DescriptorHelper.CreateDescriptor(
        AnalyzerGroups.EssentialAnalyzers,
        1021,
        isEnabledByDefault: true,
        title: "Only one Sailfish lifecycle attribute is allowed per method",
        description: "Method '{0}' may only be decorated with a single Sailfish lifecycle attribute",
        severity: DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor SuppressNonNullablePropertiesNotSetRule = DescriptorHelper.CreateDescriptor(
        AnalyzerGroups.SuppressionAnalyzers,
        idValue: 7000,
        title: "NonNullableSuppression",
        description: "Suppresses warnings when a non nullable property is set in the global setup method",
        isEnabledByDefault: true,
        severity: DiagnosticSeverity.Hidden
    );
}