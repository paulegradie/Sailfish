using System;
using System.Collections.Generic;

namespace VeerPerforma.Execution
{
    public interface IParameterGridCreator
    {
        (List<string>, int[][]) GenerateParameterGrid(Type test);
    }
}