using System.Threading.Tasks;
using Sailfish.MathOps;
using Sailfish.Statistics.Tests;
using Shouldly;
using Xunit;

namespace Test.Statistics;

public class DownSampleFixture : IAsyncLifetime
{
    private double[] data = null!;
    private readonly TestPreprocessor preprocessor = new(new SailfishOutlierDetector());

    public Task InitializeAsync()
    {
        data = new[]
        {
            100.0, 100, 100, 100, 99, 100, 100, 100, 100, 100, 100, 102, 200, 43, 12, 12, 200, 43, 12, 12
        };
        return Task.CompletedTask;
    }

    [Fact]
    public void OutlierAnalysisShouldBeNull()
    {
        preprocessor.PreprocessWithDownSample(data, false).OutlierAnalysis.ShouldBeNull();
    }

    [Fact]
    public void DownSampleReturnsCorrectSize()
    {
        preprocessor.PreprocessWithDownSample(data, false, 1, 6).RawData.Length.ShouldBe(6);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeB()
    {
        preprocessor.PreprocessWithDownSample(data, false, 3, 8).RawData.Length.ShouldBe(8);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeC()
    {
        preprocessor.PreprocessWithDownSample(data, false, 10).RawData.Length.ShouldBe(10);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeD()
    {
        preprocessor.PreprocessWithDownSample(data, false, 50).RawData.Length.ShouldBe(data.Length);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeE()
    {
        preprocessor.PreprocessWithDownSample(data, false, 5, 1).RawData.Length.ShouldBe(5);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeF()
    {
        preprocessor.PreprocessWithDownSample(data, false, 10).RawData.Length.ShouldBe(10);
    }


    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}