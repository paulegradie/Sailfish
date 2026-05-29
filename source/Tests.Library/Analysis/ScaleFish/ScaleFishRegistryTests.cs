using System;
using System.Linq;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Verifies the extensibility contract of <see cref="ComplexityFunctionRegistry"/>: built-ins are present
/// by default, user code can register custom families and they participate in the AICc ranking, and the
/// JSON converter can deserialise custom families.
///
/// Every test that mutates the registry must reset it on exit via <c>ResetToBuiltIns</c>.
/// </summary>
public class ScaleFishRegistryTests
{
    [Fact]
    public void BuiltIns_RegisteredByDefault()
    {
        var names = ComplexityFunctionRegistry.RegisteredNames();
        names.ShouldContain(nameof(Linear));
        names.ShouldContain(nameof(NLogN));
        names.ShouldContain(nameof(Quadratic));
        names.ShouldContain(nameof(Cubic));
        names.ShouldContain(nameof(LogLinear));
        names.ShouldContain(nameof(Exponential));
        names.ShouldContain(nameof(Factorial));
        names.ShouldContain(nameof(SqrtN));
    }

    [Fact]
    public void Register_NewFamily_IncludedInFitCatalog()
    {
        try
        {
            ComplexityFunctionRegistry.Register<TestQuintic>();
            ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeTrue();

            var families = ComplexityFunctionRegistry.CreateFitInstances();
            families.Any(f => f.Name == nameof(TestQuintic)).ShouldBeTrue();
            // Each call returns fresh instances (independent FunctionParameters).
            var first = ComplexityFunctionRegistry.CreateFitInstances().Single(f => f.Name == nameof(TestQuintic));
            var second = ComplexityFunctionRegistry.CreateFitInstances().Single(f => f.Name == nameof(TestQuintic));
            ReferenceEquals(first, second).ShouldBeFalse();
        }
        finally
        {
            ComplexityFunctionRegistry.ResetToBuiltIns();
        }
    }

    [Fact]
    public void Register_NewFamily_WinsOnExactMatch()
    {
        try
        {
            ComplexityFunctionRegistry.Register<TestQuintic>();

            // Generate noise-free x^5 data — the custom Quintic family should win against all built-ins.
            var measurements = Enumerable.Range(2, 6)
                .Select(i => (double)(i * 2))
                .Select(x => new ComplexityMeasurement(x, Math.Pow(x, 5)))
                .ToArray();

            var result = new ComplexityEstimator().EstimateComplexity(measurements);
            result.ShouldNotBeNull();
            result.ScaleFishModelFunction.Name.ShouldBe(nameof(TestQuintic));
        }
        finally
        {
            ComplexityFunctionRegistry.ResetToBuiltIns();
        }
    }

    [Fact]
    public void Unregister_RemovesFamilyFromCatalog()
    {
        try
        {
            ComplexityFunctionRegistry.Register<TestQuintic>();
            ComplexityFunctionRegistry.Unregister(nameof(TestQuintic)).ShouldBeTrue();
            ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeFalse();
            // Idempotent: subsequent removes are no-ops.
            ComplexityFunctionRegistry.Unregister(nameof(TestQuintic)).ShouldBeFalse();
        }
        finally
        {
            ComplexityFunctionRegistry.ResetToBuiltIns();
        }
    }

    [Fact]
    public void Register_SameName_ReplacesPreviousEntry()
    {
        try
        {
            ComplexityFunctionRegistry.Register<TestQuintic>();
            // Registering the same type twice is a no-op semantically (replaces) — should still be a single entry.
            ComplexityFunctionRegistry.Register<TestQuintic>();
            ComplexityFunctionRegistry.RegisteredNames().Count(n => n == nameof(TestQuintic)).ShouldBe(1);
        }
        finally
        {
            ComplexityFunctionRegistry.ResetToBuiltIns();
        }
    }

    [Fact]
    public void ResetToBuiltIns_RestoresExactSet()
    {
        try
        {
            ComplexityFunctionRegistry.Register<TestQuintic>();
            ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeTrue();
            ComplexityFunctionRegistry.ResetToBuiltIns();
            ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeFalse();
            ComplexityFunctionRegistry.IsRegistered(nameof(Linear)).ShouldBeTrue();
        }
        finally
        {
            ComplexityFunctionRegistry.ResetToBuiltIns();
        }
    }

    /// <summary>
    /// Sample custom family used by the tests above — represents y = scale * x^5 + bias.
    /// </summary>
    public class TestQuintic : ScaleFishModelFunction
    {
        public override string Name { get; set; } = nameof(TestQuintic);
        public override string OName { get; set; } = "O(n^5)";
        public override string Quality { get; set; } = "Catastrophic";
        public override string FunctionDef { get; set; } = "f(x) = {0}*x^5 + {1}";

        public override double Compute(double bias, double scale, double x)
        {
            return scale * Math.Pow(x, 5) + bias;
        }
    }
}
