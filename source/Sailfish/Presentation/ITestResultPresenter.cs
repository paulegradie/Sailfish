using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Statistics;

namespace Sailfish.Presentation;

public interface ITestResultPresenter
{
    Task PresentResults(List<CompiledResultContainer> resultContainers, string directoryPath, bool noTrack);
}