using System.ComponentModel.DataAnnotations;

namespace Sailfish.Statistics.Tests;

public interface ITestPreprocessor
{
    double[] Preprocess(double[] input, bool useInnerQuartile);

    double[] PreprocessWithDownSample(
        double[] rawData,
        bool useInnerQuartile,
        bool downSample,
        [Range(3, int.MaxValue)] int maxArraySize,
        [Range(3, int.MaxValue)] int minArraySize = 3,
        int? seed = null);
}