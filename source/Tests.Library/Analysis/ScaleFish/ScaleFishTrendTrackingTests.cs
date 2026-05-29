using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sailfish.Analysis.ScaleFish.Trends;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Collection definition that serialises tests touching process-wide state (environment variables,
/// the file system at fixed paths). xUnit guarantees no two tests in the same Collection run in
/// parallel — critical for the env-var test below that would otherwise race with itself or with
/// other env-mutating tests.
/// </summary>
[CollectionDefinition("ScaleFishEnvVarSerial", DisableParallelization = true)]
public class ScaleFishEnvVarSerialCollection { }

/// <summary>
/// Verifies the trend-tracking primitives: history persistence, commit-SHA resolution, and the diff
/// engine that surfaces class transitions, parameter drift, and distinguishability flips.
/// </summary>
[Collection("ScaleFishEnvVarSerial")]
public class ScaleFishTrendTrackingTests
{
    [Fact]
    public void HistoryStore_WriteAndReadRoundTrip()
    {
        var dir = NewTempDir();
        try
        {
            var entry = new ComplexityHistoryEntry(
                testClassFullName: "Test.Ns.Cls",
                methodName: "M",
                propertyName: "N",
                commitSha: "abc1234",
                timestampUtc: DateTime.UtcNow,
                bestFamilyName: "Linear",
                bestFamilyOName: "O(n)",
                bestScale: 1.5,
                bestBias: 0.1,
                bestRSquared: 0.999,
                bestAicc: -100,
                akaikeWeight: 0.999,
                isDistinguishable: true,
                sampleSize: 6,
                continuousExponentB: 1.0,
                continuousExponentC: 0.0,
                cvRankAgreement: 1.0,
                bootstrapSelectionAgreement: 1.0);

            var path = ComplexityHistoryStore.Write(dir, new[] { entry }, DateTime.UtcNow, "abc1234");
            File.Exists(path).ShouldBeTrue();

            var loaded = ComplexityHistoryStore.LoadMostRecentPrior(dir);
            loaded.Count.ShouldBe(1);
            loaded[0].BestFamilyName.ShouldBe("Linear");
            loaded[0].BestScale.ShouldBe(1.5, tolerance: 1e-9);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void HistoryStore_LoadMostRecent_PicksLatestByFilename()
    {
        var dir = NewTempDir();
        try
        {
            var older = new ComplexityHistoryEntry { BestFamilyName = "Linear" };
            var newer = new ComplexityHistoryEntry { BestFamilyName = "Quadratic" };

            ComplexityHistoryStore.Write(dir, new[] { older }, new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc), "old1234");
            // Ensure filenames differ (timestamp resolution is seconds)
            System.Threading.Thread.Sleep(50);
            ComplexityHistoryStore.Write(dir, new[] { newer }, new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc), "new1234");

            var loaded = ComplexityHistoryStore.LoadMostRecentPrior(dir);
            loaded.Count.ShouldBe(1);
            loaded[0].BestFamilyName.ShouldBe("Quadratic");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void HistoryStore_MissingDir_ReturnsEmpty()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sf-trend-missing-{Guid.NewGuid():N}");
        // Don't create — make sure load is a no-op
        ComplexityHistoryStore.LoadMostRecentPrior(dir).Count.ShouldBe(0);
    }

    [Fact]
    public void ResolveCommitSha_RespectsEnvironment()
    {
        var key = "GITHUB_SHA";
        var prior = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "deadbeefcafe");
            ComplexityHistoryStore.ResolveCommitSha().ShouldBe("deadbeefcafe");
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, prior);
        }
    }

    [Fact]
    public void Diff_Stable_NoRegression()
    {
        var prev = MakeEntry("Linear", scale: 1.0, isDistinguishable: true);
        var cur = MakeEntry("Linear", scale: 1.05, isDistinguishable: true); // 5 % drift, below default 25 %
        var diff = ComplexityHistoryDiffer.Diff(new[] { prev }, new[] { cur });
        diff.Count.ShouldBe(1);
        diff[0].Kind.ShouldBe(ComplexityTransitionKind.Stable);
        diff[0].IsRegression.ShouldBeFalse();
    }

    [Fact]
    public void Diff_ClassChanged_FlaggedAsRegression()
    {
        var prev = MakeEntry("Linear");
        var cur = MakeEntry("Quadratic");
        var diff = ComplexityHistoryDiffer.Diff(new[] { prev }, new[] { cur });
        diff.Count.ShouldBe(1);
        diff[0].Kind.ShouldBe(ComplexityTransitionKind.ClassChanged);
        diff[0].IsRegression.ShouldBeTrue();
        diff[0].Summary.ShouldContain("Linear");
        diff[0].Summary.ShouldContain("Quadratic");
    }

    [Fact]
    public void Diff_ParameterDrift_FlaggedWhenAboveTolerance()
    {
        var prev = MakeEntry("Linear", scale: 1.0);
        var cur = MakeEntry("Linear", scale: 2.0); // 100 % drift, well above 25 % default
        var diff = ComplexityHistoryDiffer.Diff(new[] { prev }, new[] { cur });
        diff.Count.ShouldBe(1);
        diff[0].Kind.ShouldBe(ComplexityTransitionKind.ParameterDrift);
    }

    [Fact]
    public void Diff_DistinguishabilityFlip_FlaggedWhenFamilyAndParamsStable()
    {
        var prev = MakeEntry("Linear", scale: 1.0, isDistinguishable: true);
        var cur = MakeEntry("Linear", scale: 1.05, isDistinguishable: false); // tiny drift, flag flipped
        var diff = ComplexityHistoryDiffer.Diff(new[] { prev }, new[] { cur });
        diff.Count.ShouldBe(1);
        diff[0].Kind.ShouldBe(ComplexityTransitionKind.DistinguishabilityChanged);
    }

    [Fact]
    public void Diff_PrioritisesClassChangeOverParameterDrift()
    {
        var prev = MakeEntry("Linear", scale: 1.0);
        var cur = MakeEntry("Quadratic", scale: 10.0);
        var diff = ComplexityHistoryDiffer.Diff(new[] { prev }, new[] { cur });
        diff[0].Kind.ShouldBe(ComplexityTransitionKind.ClassChanged);
    }

    [Fact]
    public void Diff_KeyOnlyInCurrent_Skipped()
    {
        var diff = ComplexityHistoryDiffer.Diff(
            new List<ComplexityHistoryEntry>(),
            new[] { MakeEntry("Linear", key: "Brand.New.Test") });
        diff.Count.ShouldBe(0);
    }

    [Fact]
    public void Diff_KeyOnlyInPrevious_Skipped()
    {
        var diff = ComplexityHistoryDiffer.Diff(
            new[] { MakeEntry("Linear", key: "Removed.Test") },
            new List<ComplexityHistoryEntry>());
        diff.Count.ShouldBe(0);
    }

    private static ComplexityHistoryEntry MakeEntry(
        string family = "Linear",
        double scale = 1.0,
        bool isDistinguishable = true,
        string key = "Test.Ns.Cls.M.N")
    {
        var parts = key.Split('.');
        var prop = parts[^1];
        var method = parts[^2];
        var cls = string.Join(".", parts.Take(parts.Length - 2));
        return new ComplexityHistoryEntry
        {
            TestClassFullName = cls,
            MethodName = method,
            PropertyName = prop,
            CommitSha = "test1234",
            TimestampUtc = DateTime.UtcNow,
            BestFamilyName = family,
            BestFamilyOName = family,
            BestScale = scale,
            BestBias = 0,
            BestRSquared = 0.99,
            BestAicc = 0,
            AkaikeWeight = 0.99,
            IsDistinguishable = isDistinguishable,
            SampleSize = 6
        };
    }

    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sf-trend-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
