using System.Collections.Generic;
using VeerPerforma.Statistics;

namespace VeerPerforma;

public interface ITestResultPresenter
{
    void PresentResults(List<CompiledResultContainer> resultContainers);
}