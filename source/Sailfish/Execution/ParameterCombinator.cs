using System.Collections.Generic;
using System.Linq;
using Sailfish.ExtensionMethods;

namespace Sailfish.Execution
{
    internal class ParameterCombinator : IParameterCombinator
    {
        public int[][] GetAllPossibleCombos(IEnumerable<IEnumerable<int>> ints)
        {
            var strings = ints.Select(x => x.Select(x => x.ToString()));
            IEnumerable<IEnumerable<string>> combos = new[] { new string[0] };

            foreach (var inner in strings)
                combos = from c in combos
                    from i in inner
                    select ParameterCombinatorExtensionMethods.Append(c, i);

            return combos.Select(x => x.Select(int.Parse).ToArray()).ToArray();
        }
    }
}