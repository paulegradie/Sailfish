using System.Collections.Generic;

namespace Sailfish.Execution
{
    public interface IParameterCombinator
    {
        int[][] GetAllPossibleCombos(IEnumerable<IEnumerable<int>> ints);
    }
}