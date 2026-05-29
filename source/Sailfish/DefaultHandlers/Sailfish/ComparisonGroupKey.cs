using System;

namespace Sailfish.DefaultHandlers.Sailfish;

/// <summary>
/// Composite key used to group comparison-method results during result-aggregation.
/// Comparison semantics (baseline vs. N×N) are scoped to a single test class, so the
/// key must include the class type — grouping by <c>GroupName</c> alone would merge
/// same-named groups across classes and miscount baselines.
/// </summary>
internal readonly struct ComparisonGroupKey : IEquatable<ComparisonGroupKey>
{
    public ComparisonGroupKey(Type testClass, string? groupName)
    {
        TestClass = testClass;
        GroupName = groupName;
    }

    public Type TestClass { get; }
    public string? GroupName { get; }

    public bool Equals(ComparisonGroupKey other)
        => TestClass == other.TestClass && string.Equals(GroupName, other.GroupName, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is ComparisonGroupKey other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var h = TestClass?.GetHashCode() ?? 0;
            h = (h * 397) ^ (GroupName?.GetHashCode() ?? 0);
            return h;
        }
    }
}
