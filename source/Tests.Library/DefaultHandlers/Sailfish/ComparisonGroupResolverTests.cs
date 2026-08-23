using NSubstitute;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.DefaultHandlers.Sailfish;
using Sailfish.Logging;
using Shouldly;
using Xunit;

namespace Tests.Library.DefaultHandlers.Sailfish;

/// <summary>
/// Unit tests for the shared <see cref="ComparisonGroupResolver" />. Before it was extracted this reflection
/// logic was copy-pasted, private, into the markdown and CSV run-completed handlers and could only be
/// exercised end-to-end through a full notification; these tests pin its behaviour directly.
/// </summary>
public class ComparisonGroupResolverTests
{
    private readonly ComparisonGroupResolver _resolver = new(Substitute.For<ILogger>());

    private static CompiledTestCaseResultTrackingFormat ResultFor(string displayName) =>
        new() { TestCaseId = new TestCaseId(displayName) };

    [Theory]
    [InlineData("ReadmeExample.TestMethod(N: 1)", "TestMethod")]
    [InlineData("ReadmeExample.TestMethod", "TestMethod")]
    [InlineData("TestMethod(N: 1)", "TestMethod")]
    [InlineData("TestMethod", "TestMethod")]
    public void GetMethodName_StripsClassPrefixAndVariableSuffix(string displayName, string expected)
    {
        ComparisonGroupResolver.GetMethodName(displayName).ShouldBe(expected);
    }

    [Fact]
    public void ImplicitClassWideGroup_YieldsEmptyStringGroupAndIsAMember()
    {
        var result = ResultFor($"{nameof(ImplicitGroupClass)}.MethodA(N: 1)");

        _resolver.HasComparisonGroup(result, typeof(ImplicitGroupClass)).ShouldBeTrue();
        // Empty-string sentinel = the implicit class-wide group.
        _resolver.GetComparisonGroup(result, typeof(ImplicitGroupClass)).ShouldBe(string.Empty);
    }

    [Fact]
    public void DisableComparisonClass_WithoutExplicitGroup_IsNotAMember()
    {
        var result = ResultFor($"{nameof(DisabledComparisonClass)}.MethodA(N: 1)");

        _resolver.HasComparisonGroup(result, typeof(DisabledComparisonClass)).ShouldBeFalse();
        _resolver.GetComparisonGroup(result, typeof(DisabledComparisonClass)).ShouldBeNull();
    }

    [Fact]
    public void ExplicitComparisonGroup_WinsEvenWhenClassDisablesComparison()
    {
        var result = ResultFor($"{nameof(DisabledComparisonClass)}.Explicit(N: 1)");

        _resolver.GetComparisonGroup(result, typeof(DisabledComparisonClass)).ShouldBe("Grp");
    }

    [Fact]
    public void GetComparisonInfoForResult_ReadsBaselineFlag()
    {
        var baseline = ResultFor($"{nameof(BaselineGroupClass)}.TheBaseline(N: 1)");
        var contender = ResultFor($"{nameof(BaselineGroupClass)}.Contender(N: 1)");

        _resolver.GetComparisonInfoForResult(baseline, typeof(BaselineGroupClass)).IsBaseline.ShouldBeTrue();
        _resolver.GetComparisonInfoForResult(contender, typeof(BaselineGroupClass)).IsBaseline.ShouldBeFalse();
    }

    [Fact]
    public void UnresolvableMethod_IsNotAMember()
    {
        var result = ResultFor($"{nameof(ImplicitGroupClass)}.NoSuchMethod(N: 1)");

        _resolver.HasComparisonGroup(result, typeof(ImplicitGroupClass)).ShouldBeFalse();
    }

    // ---- fixtures ----

    [Sailfish]
    private class ImplicitGroupClass
    {
        [SailfishMethod]
        public void MethodA() { }
    }

    [Sailfish(DisableComparison = true)]
    private class DisabledComparisonClass
    {
        [SailfishMethod]
        public void MethodA() { }

        [SailfishMethod(ComparisonGroup = "Grp")]
        public void Explicit() { }
    }

    [Sailfish]
    private class BaselineGroupClass
    {
        [SailfishMethod(IsBaseline = true)]
        public void TheBaseline() { }

        [SailfishMethod]
        public void Contender() { }
    }
}
