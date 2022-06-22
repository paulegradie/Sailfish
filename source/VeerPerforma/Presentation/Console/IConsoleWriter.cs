using System.Collections.Generic;
using VeerPerforma.Statistics;

namespace VeerPerforma.Presentation.Console;

public interface IConsoleWriter
{
    string Present(List<CompiledResultContainer> result);
}