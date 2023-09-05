using System.ComponentModel.DataAnnotations;

namespace Sailfish.Statistics.Tests;

public interface ITestPreprocessor
{
    PreprocessedData Preprocess(double[] input, bool useOutlierDetection);

    PreprocessedData PreprocessWithDownSample(
        double[] rawData,
        bool useOutlierDetection,
        [Range(3, int.MaxValue)] int minArraySize = 3,
        [Range(3, int.MaxValue)] int maxArraySize = 10,
        int? seed = null);

    (PreprocessedData, PreprocessedData) PreprocessJointlyWithDownSample(
        double[] sample1,
        double[] sample2,
        bool useOutlierDetection,
        [Range(3, int.MaxValue)] int minArraySize = 3,
        [Range(3, int.MaxValue)] int maxArraySize = 10,
        int? seed = null);
}