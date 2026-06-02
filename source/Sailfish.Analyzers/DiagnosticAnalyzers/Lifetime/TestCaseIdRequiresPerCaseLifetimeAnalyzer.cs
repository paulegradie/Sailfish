using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.Lifetime;

/// <summary>
///     SF1024 — a <c>[Sailfish]</c> test class that injects <c>TestCaseId</c> into its constructor must opt into
///     <c>Lifetime = SailfishLifetime.PerCase</c>.
///     <para>
///         <c>TestCaseId</c> identifies a single test case (one method × one variable combination). Under the
///         default <c>SharedInstance</c> lifetime the constructor runs <b>once</b> for the whole class, so a
///         constructor-injected <c>TestCaseId</c> would be a single class-level value rather than the per-case id
///         the test expects — a silently misleading result. <c>PerCase</c> resolves a fresh instance (and a fresh
///         <c>TestCaseId</c>) per case.
///     </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestCaseIdRequiresPerCaseLifetimeAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        id: "SF1024",
        title: "TestCaseId injection requires PerCase lifetime",
        messageFormat:
        "Test class '{0}' injects TestCaseId but uses the SharedInstance lifetime, where the constructor runs once per class — the injected TestCaseId would be a single class-level value, not the per-case id. Set [Sailfish(Lifetime = SailfishLifetime.PerCase)], or stop injecting TestCaseId.",
        category: AnalyzerGroups.EssentialAnalyzers.Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        description:
        "TestCaseId identifies a single case (method × variable combination). With the default SharedInstance lifetime the test-class constructor is invoked once for the whole class, so a constructor-injected TestCaseId cannot identify a case. Opt into Lifetime = SailfishLifetime.PerCase to get a fresh instance — and a correct TestCaseId — per case.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.IsASailfishTestType(semanticModel)) return;

        var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
        if (typeSymbol is null) return;

        // Lifetime is read from THIS class's own [Sailfish] attribute. A class that is a Sailfish test only by
        // inheritance (the attribute lives on a base) has no Lifetime of its own to evaluate here.
        var sailfishAttribute = typeSymbol
            .GetAttributes()
            .FirstOrDefault(attribute => attribute.AttributeClass?.Name == "SailfishAttribute");
        if (sailfishAttribute is null) return;

        // Only PerCase makes constructor-injected TestCaseId meaningful. The default (and SharedInstance) do not.
        if (IsPerCase(sailfishAttribute)) return;

        foreach (var parameter in EnumerateConstructorParameters(classDeclaration))
        {
            if (parameter.Type is null) continue;
            if (semanticModel.GetTypeInfo(parameter.Type).Type is not { } parameterType) continue;
            if (parameterType.Name != "TestCaseId") continue;

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, parameter.GetLocation(), typeSymbol.Name));
        }
    }

    private static bool IsPerCase(AttributeData sailfishAttribute)
    {
        foreach (var namedArgument in sailfishAttribute.NamedArguments)
        {
            if (namedArgument.Key != "Lifetime") continue;

            // An enum named-argument surfaces as its underlying integer value: SharedInstance == 0, PerCase == 1.
            return namedArgument.Value.Value is int value && value == 1;
        }

        return false; // absent => default SharedInstance
    }

    private static IEnumerable<ParameterSyntax> EnumerateConstructorParameters(TypeDeclarationSyntax classDeclaration)
    {
        foreach (var constructor in classDeclaration.Members.OfType<ConstructorDeclarationSyntax>())
            foreach (var parameter in constructor.ParameterList.Parameters)
                yield return parameter;

        // Primary constructor (C# 12+), if the class declares one.
        if (classDeclaration is ClassDeclarationSyntax { ParameterList: { } primaryConstructorParameters })
            foreach (var parameter in primaryConstructorParameters.Parameters)
                yield return parameter;
    }
}
