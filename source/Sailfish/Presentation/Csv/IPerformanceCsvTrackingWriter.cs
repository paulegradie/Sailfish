using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Statistics;

namespace Sailfish.Presentation.Csv;

public interface IPerformanceCsvTrackingWriter
{
    Task Present(List<CompiledResultContainer> result, string filePath, bool noTrack);
}