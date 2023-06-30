using Microsoft.CodeAnalysis;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.Utils;

public static class Descriptors
{
    public static DiagnosticDescriptor PropertiesMustHavePublicSettersDescriptor = DescriptorHelper.CreateDescriptor(
        AnalyzerGroups.EssentialAnalyzers,
        1001,
        title: "Test class properties set outside ctor must have public setters",
        description:
        "Properties that are set outside of class constructors are transferred between instances using reflection. Property setters must be public for this to work correctly",
        severity: DiagnosticSeverity.Error);


}