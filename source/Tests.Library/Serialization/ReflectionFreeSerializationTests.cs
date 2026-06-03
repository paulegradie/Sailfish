using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Serialization;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Results;
using Shouldly;
using Xunit;

namespace Tests.Library.Serialization;

/// <summary>
///     Regression tests for #291: the tracking-file and reproducibility-manifest contracts must (de)serialize
///     without any reflection-based resolver, so a Sailfish suite run from a Native-AOT / trimmed / .NET 10
///     file-based host (where <c>JsonSerializer.IsReflectionEnabledByDefault</c> is false) no longer crashes
///     at the end of the run.
///     <para>
///         Rather than flipping a process-global feature switch, these tests pin the options'
///         <c>TypeInfoResolver</c> to the source-gen <see cref="SailfishJsonContext" /> alone — the exact
///         configuration <see cref="SailfishSerializer" /> uses in a reflection-disabled host. If any type in
///         the persisted graph is missing from the context, the round-trip throws and the test fails.
///     </para>
/// </summary>
public class ReflectionFreeSerializationTests
{
    private static List<ClassExecutionSummaryTrackingFormat> BuildTrackingSample()
    {
        var performance = new PerformanceRunResultTrackingFormat(
            displayName: "TrackingSample.Method(N: 100)",
            mean: 12.5,
            median: 12.0,
            stdDev: 1.25,
            variance: 1.5625,
            rawExecutionResults: new[] { 11.0, 12.0, 13.0 },
            sampleSize: 3,
            numWarmupIterations: 2,
            dataWithOutliersRemoved: new[] { 11.0, 12.0, 13.0 },
            upperOutliers: Array.Empty<double>(),
            lowerOutliers: Array.Empty<double>(),
            totalNumOutliers: 0);

        // Display name carries a variable section so the TestCaseId -> TestCaseName -> TestCaseVariables ->
        // TestCaseVariable(object Value) chain is exercised end to end.
        var testCaseId = new TestCaseId("TrackingSample.Method(N: 100)");

        var compiled = new CompiledTestCaseResultTrackingFormat(
            groupingId: "TrackingSample",
            performanceRunResult: performance,
            exception: null,
            testCaseId: testCaseId);

        var settings = new ExecutionSettingsTrackingFormat(
            asCsv: true, asConsole: true, asMarkdown: false, numWarmupIterations: 2, sampleSize: 3, disableOverheadEstimation: false);

        var summary = new ClassExecutionSummaryTrackingFormat(
            typeof(ReflectionFreeSerializationTests),
            settings,
            new[] { compiled });

        return new List<ClassExecutionSummaryTrackingFormat> { summary };
    }

    private static void AssertMatchesSample(IReadOnlyList<ClassExecutionSummaryTrackingFormat>? roundTripped)
    {
        roundTripped.ShouldNotBeNull();
        roundTripped!.Count.ShouldBe(1);

        var summary = roundTripped[0];
        summary.TestClass.ShouldBe(typeof(ReflectionFreeSerializationTests));
        // SampleSize has a public setter so it round-trips. (The other ExecutionSettings flags use private
        // setters and are not repopulated on read — a pre-existing behaviour, identical under both the
        // reflection and source-gen resolvers, so it is out of scope for this reflection-freeness change.)
        summary.ExecutionSettings.SampleSize.ShouldBe(3);

        var cases = summary.CompiledTestCaseResults.ToList();
        cases.Count.ShouldBe(1);

        var only = cases[0];
        only.GroupingId.ShouldBe("TrackingSample");
        only.PerformanceRunResult.ShouldNotBeNull();
        only.PerformanceRunResult!.Mean.ShouldBe(12.5);
        only.PerformanceRunResult.RawExecutionResults.ShouldBe(new[] { 11.0, 12.0, 13.0 });

        // The TestCaseId graph (incl. the object-typed variable value) must survive the round-trip — this is
        // what ScaleFish groups observations by, so a faithful round-trip matters.
        only.TestCaseId.ShouldNotBeNull();
        only.TestCaseId!.TestCaseName.Name.ShouldBe("TrackingSample.Method");
        only.TestCaseId.TestCaseVariables.Variables.Single().Name.ShouldBe("N");
    }

    [Fact]
    public void TrackingGraph_RoundTrips_ThroughSailfishSerializer()
    {
        // Functional baseline: the normal SailfishSerializer path (source-gen + reflection fallback on hosts
        // that allow it) round-trips the full graph.
        var sample = BuildTrackingSample();
        var json = SailfishSerializer.Serialize(sample);
        var roundTripped = SailfishSerializer.Deserialize<List<ClassExecutionSummaryTrackingFormat>>(json);
        AssertMatchesSample(roundTripped);
    }

    [Fact]
    public void TrackingGraph_RoundTrips_WithSourceGenResolverOnly()
    {
        // AOT proof: pin the resolver to the source-gen context with NO reflection fallback. This is the
        // configuration used in Native-AOT / trimmed / file-based hosts. If any contract type is missing
        // from SailfishJsonContext, serialize or deserialize throws here.
        var options = SourceGenOnlyTrackingOptions();

        var sample = BuildTrackingSample();
        var json = JsonSerializer.Serialize(sample, options);
        var roundTripped = JsonSerializer.Deserialize<List<ClassExecutionSummaryTrackingFormat>>(json, options);
        AssertMatchesSample(roundTripped);
    }

    [Fact]
    public void ReproducibilityManifest_RoundTrips_WithSourceGenResolverOnly()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = SailfishJsonContext.Default
        };

        var manifest = new ReproducibilityManifest
        {
            SailfishVersion = "1.2.3",
            DotNetRuntime = ".NET 10.0",
            Os = "TestOS",
            SessionId = "session-1",
            TimestampUtc = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc),
            Tags = new Dictionary<string, string> { ["env"] = "ci", ["branch"] = "main" }
        };
        manifest.Methods.Add(new ReproducibilityManifest.MethodSnapshot
        {
            TestCaseDisplayName = "Sample.Method",
            SampleSize = 3,
            NumWarmupIterations = 2,
            Mean = 12.5,
            StdDev = 1.25,
            Ci95MarginOfError = 0.4,
            Ci99MarginOfError = 0.6
        });

        var json = JsonSerializer.Serialize(manifest, options);
        var roundTripped = JsonSerializer.Deserialize<ReproducibilityManifest>(json, options);

        roundTripped.ShouldNotBeNull();
        roundTripped!.SailfishVersion.ShouldBe("1.2.3");
        roundTripped.SessionId.ShouldBe("session-1");
        roundTripped.Tags["env"].ShouldBe("ci");
        roundTripped.Methods.Single().TestCaseDisplayName.ShouldBe("Sample.Method");
        roundTripped.Methods.Single().Mean.ShouldBe(12.5);
    }

    private static JsonSerializerOptions SourceGenOnlyTrackingOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = SailfishJsonContext.Default
        };
        // Mirror SailfishSerializer's converter set so the source-gen-only round-trip matches the real path.
        foreach (var converter in SailfishSerializer.GetDefaultConverters()) options.Converters.Add(converter);
        return options;
    }
}
