using System.IO;
using System.Threading.Tasks;
using Sailfish.Statistics.StatisticalAnalysis;

namespace Sailfish.Presentation;

public interface IBeforeAndAfterStreamReader
{
    Task<BeforeAndAfterTrackingFiles> ReadBeforeAndAfterStream(FileStream before, FileStream after);
}