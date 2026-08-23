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
    // A '.' inside a variable value must not be mistaken for the class-name separator.
    [InlineData("ReadmeExample.TestMethod(N: 1.5)", "TestMethod")]
    [InlineData("TestMethod(D: 1.5, S: a.b)", "TestMethod")]
    public void GetMethodName_StripsClassPrefixAndVariableSuffix(string displayName, string expected)
    {
        ComparisonGroupResolver.GetMethodName(displayName).ShouldBe(expected);
    }

    [Fact]
    public void ResolveMethod_MatchesCaseInsensitively()
    {
        // Fallback path: a case-differing method name still resolves to its group (the documented intent).
        var result = ResultFor($"{nameof(ImplicitGroupClass)}.methoda(N: 1)");

        _resolver.HasComparisonGroup(result, typeof(ImplicitGroupClass)).ShouldBeTrue();
    }

    [Fact]
    public void UnresolvableMethod_DoesNotFalselyMatchAPrefixNamedMethod()
    {
        // "PrefixExample" starts with the method name "Prefix"; the old displayName.StartsWith(m.Name)
        // fallback would wrongly resolve an unknown method to "Prefix" and report its group.
        var result = ResultFor($"{nameof(PrefixExample)}.Ghost(N: 1)");

        _resolver.HasComparisonGroup(result, typeof(PrefixExample)).ShouldBeFalse();
        _resolver.GetComparisonGroup(result, typeof(PrefixExample)).ShouldBeNull();
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

    // Class name starts with the method name "Prefix", to exercise the wrong-method fallback hazard.
    [Sailfish]
    private class PrefixExample
    {
        [SailfishMethod(ComparisonGroup = "P")]
        public void Prefix() { }

        [SailfishMethod(ComparisonGroup = "R")]
        public void Runner() { }
    }
}
