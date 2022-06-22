using System.Collections.Generic;
using System.Threading.Tasks;
using VeerPerforma.Statistics;

namespace VeerPerforma.Presentation;

public interface ITestResultPresenter
{
    Task PresentResults(List<CompiledResultContainer> resultContainers, string directoryPath, bool noTrack);
}