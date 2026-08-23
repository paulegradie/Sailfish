using System;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Logging;

namespace Sailfish.DefaultHandlers.Sailfish;

/// <summary>
/// Resolves comparison-group membership for a persisted test result by reflecting over its test class —
/// mapping a test-case display name back to its <see cref="MethodInfo" /> and reading the
/// <c>[SailfishMethod]</c>/<c>[Sailfish]</c> attributes that define the group and baseline. Shared by the
/// markdown and CSV run-completed handlers so both surfaces agree on which methods form a comparison cohort;
/// it was previously copy-pasted, verbatim, into each.
/// </summary>
internal sealed class ComparisonGroupResolver
{
    private readonly ILogger _logger;

    public ComparisonGroupResolver(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Extracts the bare method name from a test-case display name like "ReadmeExample.TestMethod(N: 1)".</summary>
    public static string GetMethodName(string displayName)
    {
        var methodName = displayName;

        // Remove class name prefix if present
        var dotIndex = methodName.LastIndexOf('.');
        if (dotIndex > 0)
        {
            methodName = methodName.Substring(dotIndex + 1);
        }

        // Remove any variable parameters from the display name
        var parenIndex = methodName.IndexOf('(');
        if (parenIndex > 0)
        {
            methodName = methodName.Substring(0, parenIndex);
        }

        return methodName;
    }

    /// <summary>
    /// Returns true when the test result belongs to a comparison group — either an explicit
    /// <c>ComparisonGroup</c> on the method, or the implicit class-wide group (when the enclosing
    /// <c>[Sailfish]</c> class does not set <c>DisableComparison = true</c>).
    /// </summary>
    public bool HasComparisonGroup(CompiledTestCaseResultTrackingFormat testResult, Type testClass)
    {
        return GetComparisonGroup(testResult, testClass) != null;
    }

    /// <summary>
    /// Returns the comparison-group label for a test result:
    ///   <list type="bullet">
    ///     <item><description><c>null</c> — the method is not in any comparison group (e.g. its class is <c>DisableComparison = true</c> and the method has no explicit group).</description></item>
    ///     <item><description>empty string — the method is in the implicit class-wide group.</description></item>
    ///     <item><description>non-empty string — the method's explicit <c>ComparisonGroup</c>.</description></item>
    ///   </list>
    /// </summary>
    public string? GetComparisonGroup(CompiledTestCaseResultTrackingFormat testResult, Type testClass)
    {
        try
        {
            var method = ResolveMethod(testResult, testClass);
            return method != null ? ReadComparisonInfo(method, testClass).Group : null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to get comparison group for test '{0}': {1}",
                testResult.TestCaseId?.DisplayName ?? "Unknown", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Returns the comparison info (group + baseline flag) for a single test result, doing the same
    /// method lookup as <see cref="GetComparisonGroup" />.
    /// </summary>
    public (string? Group, bool IsBaseline) GetComparisonInfoForResult(
        CompiledTestCaseResultTrackingFormat testResult, Type testClass)
    {
        try
        {
            var method = ResolveMethod(testResult, testClass);
            return method != null ? ReadComparisonInfo(method, testClass) : (null, false);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to resolve comparison info for test '{0}': {1}",
                testResult.TestCaseId?.DisplayName ?? "Unknown", ex.Message);
            return (null, false);
        }
    }

    /// <summary>
    /// Resolves the comparison group (with implicit-group semantics) + baseline flag for a method.
    /// See <see cref="GetComparisonGroup" /> for the group-string semantics.
    /// </summary>
    private static (string? Group, bool IsBaseline) ReadComparisonInfo(MethodInfo method, Type testClass)
    {
        var methodAttr = method.GetCustomAttribute<SailfishMethodAttribute>();
        if (methodAttr is null) return (null, false);

        // Explicit ComparisonGroup wins regardless of class-level setting.
        if (!string.IsNullOrEmpty(methodAttr.ComparisonGroup))
        {
            return (methodAttr.ComparisonGroup, methodAttr.IsBaseline);
        }

        // No explicit group → method joins the implicit class-wide group unless the class opted out.
        var classAttr = testClass.GetCustomAttribute<SailfishAttribute>();
        if (classAttr is not null && !classAttr.DisableComparison)
        {
            // Empty-string sentinel = implicit class-wide group.
            return (string.Empty, methodAttr.IsBaseline);
        }

        return (null, methodAttr.IsBaseline);
    }

    /// <summary>
    /// Finds the <see cref="MethodInfo" /> on <paramref name="testClass" /> that corresponds to the
    /// test case display name, accounting for variable-suffixed names and case differences.
    /// </summary>
    private static MethodInfo? ResolveMethod(CompiledTestCaseResultTrackingFormat testResult, Type testClass)
    {
        var displayName = testResult.TestCaseId?.DisplayName ?? "Unknown";
        var methodName = GetMethodName(displayName);

        var method = testClass.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        if (method != null) return method;

        var allMethods = testClass.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        return allMethods.FirstOrDefault(m =>
            string.Equals(m.Name, methodName, StringComparison.Ordinal) ||
            displayName.StartsWith(m.Name, StringComparison.Ordinal));
    }
}
