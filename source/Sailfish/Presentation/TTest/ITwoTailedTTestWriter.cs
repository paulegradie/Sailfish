using System.Threading.Tasks;
using Sailfish.Contracts.Public;
using Sailfish.Statistics.StatisticalAnalysis;

namespace Sailfish.Presentation.TTest;

internal interface ITwoTailedTTestWriter
{
    Task<TTestResultFormats> ComputeAndConvertToStringContent(BeforeAndAfterTrackingFiles beforeAndAfter, TTestSettings settings);
}