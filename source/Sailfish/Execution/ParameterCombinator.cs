using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.ExtensionMethods;

namespace Sailfish.Execution;

internal class ParameterCombinator : IParameterCombinator
{
    public int[][] GetAllPossibleCombos(IEnumerable<IEnumerable<int>> ints)
    {
        var strings = ints.Select(x => x.Select(y => y.ToString()));
        IEnumerable<IEnumerable<string>> combos = new[] { Array.Empty<string>() };

        combos = strings
            .Aggregate(
                combos,
                (current, inner) =>
                    from c
                        in current
                    from i
                        in inner
                    select ParameterCombinatorExtensionMethods.Append(c, i));

        return combos.Select(x => x.Select(int.Parse).ToArray()).ToArray();
    }
}