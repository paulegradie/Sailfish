using System.Collections.Generic;
using System.Threading.Tasks;
using VeerPerforma.Statistics;

namespace VeerPerforma.Presentation.Csv;

public interface IPerformanceCsvTrackingWriter
{
    Task Present(List<CompiledResultContainer> result, string filePath, bool noTrack);
}