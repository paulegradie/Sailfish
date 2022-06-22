using System.Threading.Tasks;

namespace Sailfish.Presentation.TTest;

public interface ITwoTailedTTestWriter
{
    Task PresentTestResults(string readDirectory, string outputPath, TTestSettings settings);
}