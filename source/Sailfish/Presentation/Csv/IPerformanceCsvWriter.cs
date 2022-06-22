using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Statistics;

namespace Sailfish.Presentation.Csv;

public interface IPerformanceCsvWriter
{
    Task Present(List<CompiledResultContainer> result, string filePath);
}