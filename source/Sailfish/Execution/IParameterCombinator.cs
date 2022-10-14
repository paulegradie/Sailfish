using System.Collections.Generic;

namespace Sailfish.Execution;

internal interface IParameterCombinator
{
    int[][] GetAllPossibleCombos(IEnumerable<IEnumerable<int>> ints);
}