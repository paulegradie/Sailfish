using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
/// Every test that mutates the catalog does so inside <see cref="ComplexityFunctionRegistry.BeginIsolatedScope"/>,
/// so the mutations are confined to this test's async flow. The test suite runs collections in parallel;
/// mutating the process-global catalog here would race every concurrently-running test that fits
/// measurements (observed as a one-off failure of ScaleFishParallelBootstrapTests.RepeatedParallelRuns_AreIdentical
/// under load).
/// </summary>
public class ScaleFishRegistryTests
{
    [Fact]
    public void BuiltIns_RegisteredByDefault()
    {
        var names = ComplexityFunctionRegistry.RegisteredNames();
        names.ShouldContain(nameof(Constant));
        names.ShouldContain(nameof(Logarithmic));
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
    public void LogLinear_IsDeserializationOnly()
    {
        // Registered (old model files keep loading)…
        ComplexityFunctionRegistry.IsRegistered(nameof(LogLinear)).ShouldBeTrue();
        var elem = System.Text.Json.JsonSerializer.SerializeToElement(new LogLinear());
        ComplexityFunctionRegistry.Deserialize(nameof(LogLinear), elem).ShouldNotBeNull();

        // …but never a fit candidate: its basis is a constant multiple of NLogN's, and fitting both
        // made every n·log n classification tie with its own clone (never distinguishable).
        ComplexityFunctionRegistry.CreateFitInstances()
            .Any(f => f.Name == nameof(LogLinear))
            .ShouldBeFalse("LogLinear is collinear with NLogN and must not participate in fitting");
    }

    [Fact]
    public void Register_LogLinear_OptsBackIntoFitting()
    {
        using var scope = ComplexityFunctionRegistry.BeginIsolatedScope();

        // The escape hatch: explicit registration restores the family as a fit candidate.
        ComplexityFunctionRegistry.Register<LogLinear>();
        ComplexityFunctionRegistry.CreateFitInstances()
            .Any(f => f.Name == nameof(LogLinear))
            .ShouldBeTrue();
    }

    [Fact]
    public void Register_NewFamily_IncludedInFitCatalog()
    {
        using var scope = ComplexityFunctionRegistry.BeginIsolatedScope();

        ComplexityFunctionRegistry.Register<TestQuintic>();
        ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeTrue();

        var families = ComplexityFunctionRegistry.CreateFitInstances();
        families.Any(f => f.Name == nameof(TestQuintic)).ShouldBeTrue();
        // Each call returns fresh instances (independent FunctionParameters).
        var first = ComplexityFunctionRegistry.CreateFitInstances().Single(f => f.Name == nameof(TestQuintic));
        var second = ComplexityFunctionRegistry.CreateFitInstances().Single(f => f.Name == nameof(TestQuintic));
        ReferenceEquals(first, second).ShouldBeFalse();
    }

    [Fact]
    public void Register_NewFamily_WinsOnExactMatch()
    {
        using var scope = ComplexityFunctionRegistry.BeginIsolatedScope();

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

    [Fact]
    public void Unregister_RemovesFamilyFromCatalog()
    {
        using var scope = ComplexityFunctionRegistry.BeginIsolatedScope();

        ComplexityFunctionRegistry.Register<TestQuintic>();
        ComplexityFunctionRegistry.Unregister(nameof(TestQuintic)).ShouldBeTrue();
        ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeFalse();
        // Idempotent: subsequent removes are no-ops.
        ComplexityFunctionRegistry.Unregister(nameof(TestQuintic)).ShouldBeFalse();
    }

    [Fact]
    public void Register_SameName_ReplacesPreviousEntry()
    {
        using var scope = ComplexityFunctionRegistry.BeginIsolatedScope();

        ComplexityFunctionRegistry.Register<TestQuintic>();
        // Registering the same type twice is a no-op semantically (replaces) — should still be a single entry.
        ComplexityFunctionRegistry.Register<TestQuintic>();
        ComplexityFunctionRegistry.RegisteredNames().Count(n => n == nameof(TestQuintic)).ShouldBe(1);
    }

    [Fact]
    public void ResetToBuiltIns_RestoresExactSet()
    {
        using var scope = ComplexityFunctionRegistry.BeginIsolatedScope();

        ComplexityFunctionRegistry.Register<TestQuintic>();
        ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeTrue();
        ComplexityFunctionRegistry.ResetToBuiltIns();
        ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeFalse();
        ComplexityFunctionRegistry.IsRegistered(nameof(Linear)).ShouldBeTrue();
    }

    [Fact]
    public void Register_KeysByRuntimeName_NotTypeName()
    {
        // A custom family whose `Name` deliberately diverges from its C# type name. The registry must
        // key by the runtime Name (what the JSON writer emits) so the converter can find it on load.
        using var scope = ComplexityFunctionRegistry.BeginIsolatedScope();

        ComplexityFunctionRegistry.Register<RenamedFamily>();

        ComplexityFunctionRegistry.IsRegistered("CustomLogLog").ShouldBeTrue("key should be the runtime Name");
        ComplexityFunctionRegistry.IsRegistered(nameof(RenamedFamily)).ShouldBeFalse("type-name key must not leak");

        // Deserialize should succeed for the runtime Name…
        var elem = System.Text.Json.JsonSerializer.SerializeToElement(new RenamedFamily());
        ComplexityFunctionRegistry.Deserialize("CustomLogLog", elem).ShouldNotBeNull();

        // …and return null when called with the type name (no false positives).
        ComplexityFunctionRegistry.Deserialize(nameof(RenamedFamily), elem).ShouldBeNull();
    }

    // ─── Scope isolation ───────────────────────────────────────────────────────────────

    [Fact]
    public void IsolatedScope_MutationsAreInvisibleToOtherFlows()
    {
        using var scope = ComplexityFunctionRegistry.BeginIsolatedScope();
        ComplexityFunctionRegistry.IsScopeActive.ShouldBeTrue("scope must be active on the registering flow");
        ComplexityFunctionRegistry.Register<TestQuintic>();
        ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeTrue("the registering flow sees its own mutation");

        // Observe the catalog from a flow that did NOT inherit this scope's ExecutionContext —
        // exactly how a concurrently-running test sees it. A dedicated thread with flow suppressed
        // is the only airtight probe: Task.Run + a blocking wait can INLINE the task onto this very
        // thread, where an unflowed delegate executes under the thread's ambient (scoped) context.
        var seenElsewhere = true;
        using (ExecutionContext.SuppressFlow())
        {
            var probe = new Thread(() => seenElsewhere = ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)));
            probe.Start();
            probe.Join();
        }

        seenElsewhere.ShouldBeFalse("scoped registrations must not leak into other async flows");
    }

    [Fact]
    public void IsolatedScope_DisposeRestoresThePreviousView()
    {
        ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeFalse();

        using (ComplexityFunctionRegistry.BeginIsolatedScope())
        {
            ComplexityFunctionRegistry.Register<TestQuintic>();

            // Nested scope snapshots the outer scope's view and discards its own changes on dispose.
            using (ComplexityFunctionRegistry.BeginIsolatedScope())
            {
                ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeTrue("nested scope inherits the outer view");
                ComplexityFunctionRegistry.Unregister(nameof(TestQuintic)).ShouldBeTrue();
                ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeFalse();
            }

            ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeTrue("outer scope unaffected by the nested scope's mutations");
        }

        ComplexityFunctionRegistry.IsRegistered(nameof(TestQuintic)).ShouldBeFalse("flow returns to the global view after the last scope disposes");
    }

    [Fact]
    public async Task ConcurrentScopedMutation_DoesNotPerturbEstimationDeterminism()
    {
        // Regression for the observed flake: a registry-mutating test running in parallel with a
        // determinism test changed the candidate catalog between that test's two estimations. With
        // scoped mutation, a hammering mutator must be invisible: two estimations on identical data
        // must classify identically while the mutation loop runs.
        var rng = new Random(99);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => 2.0 * x + 5.0,
            new[] { 8, 16, 32, 64, 128, 256 },
            sampleSize: 12,
            relativeNoise: 0.05,
            rng);

        using var hammerStarted = new ManualResetEventSlim(false);
        using var stopHammer = new CancellationTokenSource();
        var hammer = Task.Run(() =>
        {
            using var hammerScope = ComplexityFunctionRegistry.BeginIsolatedScope();
            hammerStarted.Set();
            while (!stopHammer.Token.IsCancellationRequested)
            {
                ComplexityFunctionRegistry.Register<TestQuintic>();
                ComplexityFunctionRegistry.Unregister(nameof(TestQuintic));
                ComplexityFunctionRegistry.ResetToBuiltIns();
            }
        });

        try
        {
            hammerStarted.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue("mutation hammer failed to start");

            var estimator = new ComplexityEstimator();
            var first = estimator.EstimateComplexity(measurements);
            var second = estimator.EstimateComplexity(measurements);

            first.ShouldNotBeNull();
            second.ShouldNotBeNull();
            second.ScaleFishModelFunction.Name.ShouldBe(first.ScaleFishModelFunction.Name);
            second.BestAicc.ShouldBe(first.BestAicc);
            second.AkaikeWeight.ShouldBe(first.AkaikeWeight);
            second.IsDistinguishable.ShouldBe(first.IsDistinguishable);
        }
        finally
        {
            stopHammer.Cancel();
            await hammer;
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

    /// <summary>
    /// Custom family whose <see cref="ScaleFishModelFunction.Name"/> diverges from the CLR type name.
    /// Exercises the registry's contract that registration keys by runtime Name, not <c>typeof(T).Name</c>.
    /// </summary>
    public class RenamedFamily : ScaleFishModelFunction
    {
        public override string Name { get; set; } = "CustomLogLog";
        public override string OName { get; set; } = "O(log log n)";
        public override string Quality { get; set; } = "Excellent";
        public override string FunctionDef { get; set; } = "f(x) = {0}*log(log(x)) + {1}";

        public override double Compute(double bias, double scale, double x)
            => scale * Math.Log(Math.Log(Math.Max(x, Math.E + 0.001))) + bias;
    }
}
