using Serilog.Core;
using VeerPerforma.Utils;

namespace VeerPerforma.Execution;

public interface IParameterGridCreator
{
    (List<string>, IEnumerable<IEnumerable<int>>) GenerateParameterGrid(Type test);
}