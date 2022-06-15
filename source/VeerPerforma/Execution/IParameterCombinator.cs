using System.Collections.Generic;

namespace VeerPerforma.Execution
{
    public interface IParameterCombinator
    {
        int[][] GetAllPossibleCombos(IEnumerable<IEnumerable<int>> ints);
    }
}