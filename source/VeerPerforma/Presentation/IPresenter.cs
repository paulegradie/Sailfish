using System.Collections.Generic;
using VeerPerforma.Statistics;

namespace VeerPerforma.Presentation;

public interface IConsoleWriter
{
    string Present(List<CompiledResultContainer> result);
}

public interface ICsvWriter
{
    string Present(List<CompiledResultContainer> result);
}

