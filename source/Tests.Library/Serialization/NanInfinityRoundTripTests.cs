using System.Text.Json;
using Sailfish.Contracts.Public.Serialization;
using Sailfish.Contracts.Public.Serialization.JsonConverters;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Shouldly;
using Xunit;

namespace Tests.Library.Serialization;

/// <summary>
///     Regression tests for the NaN/±Infinity round-trip bug: JsonNanConverter / InfinityConverter wrote
///     these special values as strings but their Read called Utf8JsonReader.TryGetDouble first, which THROWS
///     on a string token — so any tracking-file double that was NaN/Infinity (e.g. a single-sample Variance)
///     became unreadable.
/// </summary>
public class NanInfinityRoundTripTests
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(12.5)]
    public void JsonNanConverter_RoundTripsSpecialDoubles_ViaSailfishSerializer(double value)
    {
        // SailfishSerializer registers JsonNanConverter for every double, so this is the real persistence path.
        var json = SailfishSerializer.Serialize(value);
        var roundTripped = SailfishSerializer.Deserialize<double>(json);

        roundTripped.ShouldBe(value); // Shouldly treats NaN.ShouldBe(NaN) as equal
    }

    [Fact]
    public void PerformanceRunResult_WithNanAndInfinityFields_RoundTrips()
    {
        var original = new PerformanceRunResultTrackingFormat(
            displayName: "Sample.Method(N: 1)",
            mean: double.NaN,
            median: 10.0,
            stdDev: double.PositiveInfinity,
            variance: double.NegativeInfinity,
            rawExecutionResults: new[] { 10.0, double.NaN, 11.0 },
            sampleSize: 1,
            numWarmupIterations: 0,
            dataWithOutliersRemoved: new[] { 10.0 },
            upperOutliers: System.Array.Empty<double>(),
            lowerOutliers: System.Array.Empty<double>(),
            totalNumOutliers: 0);

        var json = SailfishSerializer.Serialize(original);
        var rt = SailfishSerializer.Deserialize<PerformanceRunResultTrackingFormat>(json);

        rt.ShouldNotBeNull();
        double.IsNaN(rt!.Mean).ShouldBeTrue();
        rt.Median.ShouldBe(10.0);
        double.IsPositiveInfinity(rt.StdDev).ShouldBeTrue();
        double.IsNegativeInfinity(rt.Variance).ShouldBeTrue();
        rt.RawExecutionResults.Length.ShouldBe(3);
        double.IsNaN(rt.RawExecutionResults[1]).ShouldBeTrue(); // the array path goes through the same converter
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(3.0)]
    public void InfinityConverter_RoundTripsInfinity(double value)
    {
        var options = new JsonSerializerOptions { Converters = { new InfinityConverter() } };

        var json = JsonSerializer.Serialize(value, options);
        var roundTripped = JsonSerializer.Deserialize<double>(json, options);

        roundTripped.ShouldBe(value);
    }

    // CodeRabbit (#316): a token that is neither a number nor a string (object/array/boolean/null) must
    // surface a deterministic JsonException, not the InvalidOperationException that Utf8JsonReader.GetString()
    // throws on a mismatched token type.
    [Theory]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void JsonNanConverter_ThrowsJsonException_OnNonNumericNonStringToken(string json)
    {
        var options = new JsonSerializerOptions { Converters = { new JsonNanConverter() } };
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<double>(json, options));
    }

    [Theory]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void InfinityConverter_ThrowsJsonException_OnNonNumericNonStringToken(string json)
    {
        var options = new JsonSerializerOptions { Converters = { new InfinityConverter() } };
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<double>(json, options));
    }
}
