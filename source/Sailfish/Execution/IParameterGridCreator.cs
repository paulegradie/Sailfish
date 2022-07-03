using System;
using System.Collections.Generic;

namespace Sailfish.Execution
{
    internal interface IParameterGridCreator
    {
        (List<string>, int[][]) GenerateParameterGrid(Type test);
    }
}