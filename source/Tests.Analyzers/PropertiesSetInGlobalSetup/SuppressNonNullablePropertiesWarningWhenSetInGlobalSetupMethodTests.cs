using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Sailfish.Analyzers.DiagnosticAnalyzers;
using System.Threading.Tasks;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.PropertiesSetInGlobalSetup;

public class SuppressNonNullablePropertiesWarningWhenSetInGlobalSetupMethodTests
{
    [Fact]
    public async Task ReportsHiddenDiagnostic_WhenNonNullablePropertyIsAssignedInGlobalSetup()
    {
        const string source = @"#nullable enable
[Sailfish]
public class Bench
{
    {|#0:public string Name { get; set; }|}

    [SailfishGlobalSetup]
    public void GlobalSetup() { Name = ""X""; }

    [SailfishMethod]
    public void Run() { }
}";

        await AnalyzerVerifier<SuppressNonNullablePropertiesWarningWhenSetInGlobalSetupMethod>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult("SF7000", DiagnosticSeverity.Hidden).WithLocation(0).WithArguments("Name"));
    }

    [Fact]
    public async Task DoesNotReport_WhenCS8618IsSuppressedInCompilationOptions()
    {
        const string source = @"#nullable enable
[Sailfish]
public class Bench
{
    public string Name { get; set; }

    [SailfishGlobalSetup]
    public void GlobalSetup() { Name = ""X""; }

    [SailfishMethod]
    public void Run() { }
}";

        var test = new CSharpAnalyzerTest<SuppressNonNullablePropertiesWarningWhenSetInGlobalSetupMethod, XUnitVerifier>
        {
            TestCode = source.AddSailfishAttributeDependencies()
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            var options = (CSharpCompilationOptions)project.CompilationOptions!;
            var updated = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItem("CS8618", ReportDiagnostic.Suppress));
            return solution.WithProjectCompilationOptions(projectId, updated);
        });

        await test.RunAsync();
    }
}

