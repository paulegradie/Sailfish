using System.Threading.Tasks;

namespace VeerPerforma.Statistics.StatisticalAnalysis;

public interface ITwoTailedTTester
{
    Task PresentTestResults(string readDirectory, string outputPath);
}