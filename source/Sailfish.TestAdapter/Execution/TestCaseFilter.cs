using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter.Execution;

/// <summary>
///     Applies the VSTest <c>--filter</c> expression (from <see cref="IRunContext.GetTestCaseFilter" />) to a set
///     of discovered Sailfish test cases. Previously the adapter accepted <see cref="IRunContext" /> but never
///     consulted it, so <c>dotnet test --filter</c> (and Test Explorer's filter box) silently ran everything.
/// </summary>
/// <remarks>
///     Supported filter properties — usable as <c>dotnet test --filter "Method=Foo"</c>,
///     <c>--filter "FullyQualifiedName~MyClass"</c>, <c>--filter "ComparisonGroup=Sort"</c>, etc.:
///     <list type="bullet">
///         <item><c>FullyQualifiedName</c> / <c>DisplayName</c> — the VSTest built-ins;</item>
///         <item><c>Namespace</c> / <c>Class</c> / <c>Method</c> — the Sailfish type and method;</item>
///         <item><c>ComparisonGroup</c> — the cross-method comparison group.</item>
///     </list>
///     Comparison group is also surfaced as a <see cref="Trait" /> at discovery, so it shows in Test Explorer.
/// </remarks>
internal static class TestCaseFilter
{
    /// <summary>How to read each supported filter property's value from a test case.</summary>
    private static readonly Dictionary<string, Func<TestCase, string?>> ValueProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FullyQualifiedName"] = tc => tc.FullyQualifiedName,
        ["DisplayName"] = tc => tc.DisplayName,
        ["Namespace"] = NamespaceOf,
        ["Class"] = ClassNameOf,
        ["Method"] = tc => tc.GetPropertyValue<string>(SailfishManagedProperty.SailfishMethodFilterProperty, null),
        ["ComparisonGroup"] = tc => tc.GetPropertyValue<string>(SailfishManagedProperty.SailfishComparisonGroupProperty, null)
    };

    /// <summary>The TestProperty backing each supported filter name (lets the platform parse/validate the filter).</summary>
    private static readonly Dictionary<string, TestProperty> PropertyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FullyQualifiedName"] = TestCaseProperties.FullyQualifiedName,
        ["DisplayName"] = TestCaseProperties.DisplayName,
        ["Namespace"] = TestProperty.Register("Sailfish.Filter.Namespace", "Namespace", typeof(string), typeof(TestCaseFilter)),
        ["Class"] = TestProperty.Register("Sailfish.Filter.Class", "Class", typeof(string), typeof(TestCaseFilter)),
        ["Method"] = TestProperty.Register("Sailfish.Filter.Method", "Method", typeof(string), typeof(TestCaseFilter)),
        ["ComparisonGroup"] = TestProperty.Register("Sailfish.Filter.ComparisonGroup", "ComparisonGroup", typeof(string), typeof(TestCaseFilter))
    };

    private static readonly string[] SupportedProperties = ValueProviders.Keys.ToArray();

    /// <summary>
    ///     Returns the test cases that satisfy the run's filter. If there is no filter (e.g. the user selected
    ///     cases explicitly in the IDE, or ran without <c>--filter</c>) all cases are returned. A malformed filter
    ///     is logged and treated as "no filter" so a typo never silently drops the whole run.
    /// </summary>
    public static List<TestCase> Filter(List<TestCase> testCases, IRunContext? runContext, IMessageLogger? logger)
    {
        if (runContext is null || testCases.Count == 0) return testCases;

        ITestCaseFilterExpression? filterExpression;
        try
        {
            filterExpression = runContext.GetTestCaseFilter(SupportedProperties, name => PropertyMap.GetValueOrDefault(name));
        }
        catch (TestPlatformFormatException ex)
        {
            logger?.SendMessage(TestMessageLevel.Warning,
                $"Ignoring an invalid Sailfish test filter and running all discovered tests. {ex.Message}");
            return testCases;
        }

        if (filterExpression is null) return testCases;

        var matched = testCases.Where(testCase => filterExpression.MatchTestCase(testCase, name => GetValue(testCase, name))).ToList();
        logger?.SendMessage(TestMessageLevel.Informational,
            $"Sailfish test filter '{filterExpression.TestCaseFilterValue}' matched {matched.Count} of {testCases.Count} test case(s).");
        return matched;
    }

    private static object? GetValue(TestCase testCase, string propertyName)
        => ValueProviders.TryGetValue(propertyName, out var read) ? read(testCase) : null;

    private static string? NamespaceOf(TestCase testCase)
    {
        var fullName = testCase.GetPropertyValue<string>(SailfishManagedProperty.SailfishTypeProperty, null);
        var lastDot = fullName?.LastIndexOf('.') ?? -1;
        return lastDot > 0 ? fullName![..lastDot] : null;
    }

    private static string? ClassNameOf(TestCase testCase)
    {
        var fullName = testCase.GetPropertyValue<string>(SailfishManagedProperty.SailfishTypeProperty, null);
        if (string.IsNullOrEmpty(fullName)) return null;
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
    }
}
