using System;
using System.Collections.Generic;

namespace Sailfish.Execution
{
    public interface IParameterGridCreator
    {
        (List<string>, int[][]) GenerateParameterGrid(Type test);
    }
}