using System.Linq;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

/// <summary>
/// Covers the joint preprocessing path used by the paired signed-rank test: outlier-aware,
/// equal-length alignment via deterministic random subsampling of the larger side. (The solo
/// down-sampling entry point was removed — it had no production callers and silently capped
/// samples at 10 when it was last wired in.)
/// </summary>
public class JointDownSampleFixture
{
    private const int Seed = 42;
    private readonly TestPreprocessor _preprocessor = new(new SailfishOutlierDetector());

    [Fact]
    public void UnequalSides_AlignToTheSmallerSide()
    {
        var sample1 = Enumerable.Range(1, 20).Select(i => (double)i).ToArray();
        var sample2 = Enumerable.Range(1, 12).Select(i => (double)i).ToArray();

        var (preprocessed1, preprocessed2) = _preprocessor.PreprocessJointlyWithDownSample(
            sample1, sample2, useOutlierDetection: false, minArraySize: 3, maxArraySize: int.MaxValue, seed: Seed);

        preprocessed1.RawData.Length.ShouldBe(12);
        preprocessed2.RawData.Length.ShouldBe(12);
        // The subsample must be drawn from the input (no fabricated values) and preserve input order
        // (indices are sorted before extraction, so output ordering is deterministic).
        preprocessed1.RawData.All(sample1.Contains).ShouldBeTrue();
        preprocessed1.RawData.ShouldBe(preprocessed1.RawData.OrderBy(v => v).ToArray());
    }

    [Fact]
    public void SeededRuns_AreDeterministic()
    {
        var sample1 = new double[] { 14, 15, 532, 52, 534, 78, 47, 732, 226, 27, 277, 234, 620, 206, 342, 623, 66, 342, 26, 342 };
        var sample2 = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();

        var (first, _) = _preprocessor.PreprocessJointlyWithDownSample(
            sample1, sample2, useOutlierDetection: false, minArraySize: 3, maxArraySize: int.MaxValue, seed: Seed);
        var (second, _) = _preprocessor.PreprocessJointlyWithDownSample(
            sample1, sample2, useOutlierDetection: false, minArraySize: 3, maxArraySize: int.MaxValue, seed: Seed);

        first.RawData.ShouldBe(second.RawData);
        first.RawData.Length.ShouldBe(10);
    }

    [Fact]
    public void EqualSides_PassThroughUntouched()
    {
        var sample1 = new double[] { 1, 2, 3, 4, 5 };
        var sample2 = new double[] { 6, 7, 8, 9, 10 };

        var (preprocessed1, preprocessed2) = _preprocessor.PreprocessJointlyWithDownSample(
            sample1, sample2, useOutlierDetection: false, minArraySize: 3, maxArraySize: int.MaxValue, seed: Seed);

        preprocessed1.RawData.ShouldBe(sample1);
        preprocessed2.RawData.ShouldBe(sample2);
        preprocessed1.OutlierAnalysis.ShouldBeNull();
        preprocessed2.OutlierAnalysis.ShouldBeNull();
    }

    [Fact]
    public void ExplicitMaxArraySize_CapsBothSides()
    {
        var sample1 = Enumerable.Range(1, 20).Select(i => (double)i).ToArray();
        var sample2 = Enumerable.Range(1, 20).Select(i => (double)i).ToArray();

        var (preprocessed1, preprocessed2) = _preprocessor.PreprocessJointlyWithDownSample(
            sample1, sample2, useOutlierDetection: false, minArraySize: 3, maxArraySize: 8, seed: Seed);

        preprocessed1.RawData.Length.ShouldBe(8);
        preprocessed2.RawData.Length.ShouldBe(8);
        // Different seed streams per side: the two sides should not be forced onto identical indices.
        preprocessed1.RawData.SequenceEqual(preprocessed2.RawData).ShouldBeFalse();
    }
}
