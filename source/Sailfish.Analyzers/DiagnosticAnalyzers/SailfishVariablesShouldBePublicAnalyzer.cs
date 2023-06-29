using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.DiagnosticAnalyzers.Utils;

namespace Sailfish.Analyzers.DiagnosticAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SailfishVariablesShouldBePublicAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor SailfishVariablesShouldBePublicDescriptor = DescriptorHelper.CreateDescriptor(
            AnalyzerGroups.EssentialAnalyzers,
            1001,
            isEnabledByDefault: true,
            title: "SailfishVariable properties should be public",
            description: "SailfishVariables are get and set using by the test framework and should therefore should be public",
            severity: DiagnosticSeverity.Error);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SailfishVariablesShouldBePublicDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            SupportedDiagnostics.Add(SailfishVariablesShouldBePublicDescriptor);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            if (!Debugger.IsAttached) context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            if (classDeclaration.AttributeLists.Count <= 0) return;

            foreach (var attributeList in classDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var attributeType = context.SemanticModel.GetTypeInfo(attribute).Type;

                    if (attributeType is not ({ Name: "Sailfish" } or { Name: "SailfishAttribute" })) continue;

                    foreach (var member in classDeclaration.Members)
                    {
                        if (member is not PropertyDeclarationSyntax propertyDeclaration) continue;
                        var variableAttribute = propertyDeclaration.AttributeLists
                            .SelectMany(list => list.Attributes)
                            .FirstOrDefault(attr => context.SemanticModel.GetTypeInfo(attr).Type?.Name == "SailfishVariableAttribute");

                        if (variableAttribute == null || propertyDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword)) continue;

                        var diagnostic = Diagnostic.Create(
                            SailfishVariablesShouldBePublicDescriptor,
                            propertyDeclaration.Identifier.GetLocation(),
                            propertyDeclaration.Identifier.Text);

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}