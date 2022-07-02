using System.Threading.Tasks;
using Sailfish.Statistics.StatisticalAnalysis;

namespace Sailfish.Presentation.TTest;

public interface ITwoTailedTTestWriter
{
    Task<string> ComputeAndConvertToStringContent(BeforeAndAfterTrackingFiles beforeAndAfter, TTestSettings settings);
}