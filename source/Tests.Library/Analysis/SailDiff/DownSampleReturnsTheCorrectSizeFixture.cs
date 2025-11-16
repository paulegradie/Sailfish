using System.Threading.Tasks;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

public class DownSampleFixture : IAsyncLifetime
{
    private readonly TestPreprocessor _preprocessor = new(new SailfishOutlierDetector());
    private double[] _data = null!;

    public Task InitializeAsync()
    {
        _data =
        [
            100.0, 100, 100, 100, 99, 100, 100, 100, 100, 100, 100, 102, 200, 43, 12, 12, 200, 43, 12, 12
        ];
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public void OutlierAnalysisShouldBeNull()
    {
        _preprocessor.PreprocessWithDownSample(_data, false).OutlierAnalysis.ShouldBeNull();
    }

    [Fact]
    public void DownSampleReturnsCorrectSize()
    {
        _preprocessor.PreprocessWithDownSample(_data, false, 1, 6).RawData.Length.ShouldBe(6);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeB()
    {
        _preprocessor.PreprocessWithDownSample(_data, false, 3, 8).RawData.Length.ShouldBe(8);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeC()
    {
        _preprocessor.PreprocessWithDownSample(_data, false, 10).RawData.Length.ShouldBe(10);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeD()
    {
        _preprocessor.PreprocessWithDownSample(_data, false, 50).RawData.Length.ShouldBe(_data.Length);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeE()
    {
        _preprocessor.PreprocessWithDownSample(_data, false, 5, 1).RawData.Length.ShouldBe(5);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeF()
    {
        _preprocessor.PreprocessWithDownSample(_data, false, 10).RawData.Length.ShouldBe(10);
    }
}