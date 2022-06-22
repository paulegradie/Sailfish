using System.Threading.Tasks;

namespace Sailfish.Statistics.StatisticalAnalysis;

public interface ITwoTailedTTester
{
    Task PresentTestResults(string readDirectory, string outputPath);
}