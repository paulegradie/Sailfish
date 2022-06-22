using System.Collections.Generic;
using System.Linq;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Test.VeerPerforma.Executor.Tests
{
    public class WhenCompilingIterationVariables
    {
        [Fact]
        public void AllCombinationsAreFound_TwoProperties()
        {
            var combinator = new ParameterCombinator();
            var combos = combinator.GetAllPossibleCombos(
                new List<List<int>>
                {
                    new List<int>() {1, 2, 3},
                    new List<int>() {4, 5},
                    new List<int>() {6, 7, 8, 9}
                });
            var result = combos.Select(c => c.ToArray()).ToArray();

            result.Length.ShouldBe(24);
        }
    }
}