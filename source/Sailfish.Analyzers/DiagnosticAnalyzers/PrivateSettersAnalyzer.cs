// using System.Collections.Immutable;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Microsoft.CodeAnalysis.Diagnostics;
// using Sailfish.Analyzers.DiagnosticAnalyzers.Utils;
// using DiagnosticDescriptor = Microsoft.CodeAnalysis.DiagnosticDescriptor;
// using LanguageNames = Microsoft.CodeAnalysis.LanguageNames;
// using static Sailfish.Analyzers.DiagnosticAnalyzers.Utils.Descriptors;
//
// namespace Sailfish.Analyzers.DiagnosticAnalyzers;
//
// [DiagnosticAnalyzer(LanguageNames.CSharp)]
// public class PrivateSettersAnalyzer : DiagnosticAnalyzerBase
// {
//     public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
//         ImmutableArray.Create(PropertiesMustHavePublicSettersDescriptor);
//
//     protected override (Action<SyntaxNodeAnalysisContext>, SyntaxKind[]) CreateAnalyzer(AnalysisContext context)
//     {
//         return (AnalyzeSyntaxNode, new[] { SyntaxKind.ClassDeclaration });
//     }
//
//     private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
//     {
//         var assignment = (AssignmentExpressionSyntax)context.Node;
//
//         if (assignment is not { Left: IdentifierNameSyntax { Identifier.ValueText: "PrivateSetter" } identifier, Right: LiteralExpressionSyntax literal } ||
//             ModelExtensions.GetSymbolInfo(context.SemanticModel, identifier).Symbol is not IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Private } ||
//             !IsWithinSailfishGlobalSetupMethod(context)) return;
//         context.ReportDiagnostic(Diagnostic.Create(PropertiesMustHavePublicSettersDescriptor, assignment.GetLocation()));
//     }
//
//     private static bool IsWithinSailfishGlobalSetupMethod(SyntaxNodeAnalysisContext context)
//     {
//         var methodDeclaration = context.Node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
//         return methodDeclaration?.AttributeLists
//             .SelectMany(list => list.Attributes)
//             .Any(attribute => attribute.Name.ToString() == "SailfishGlobalSetupAttribute") == true;
//     }
// }