using System.Collections.Generic;
using System.Threading.Tasks;
using VeerPerforma.Statistics;

namespace VeerPerforma.Presentation.Csv;

public interface IPerformanceCsvWriter
{
    Task Present(List<CompiledResultContainer> result, string filePath);
}