using System.Collections.Generic;
using System.Linq;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Test.Execution
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
                    new() { 1, 2, 3 },
                    new() { 4, 5 },
                    new() { 6, 7, 8, 9 }
                });
            var result = combos.Select(c => c.ToArray()).ToArray();

            result.Length.ShouldBe(24);
        }

        [Fact]
        public void AllCombinationsMatchExpected()
        {
            var combinator = new ParameterCombinator();
            var combos = combinator.GetAllPossibleCombos(
                new List<List<int>>
                {
                    new() { 1, 2 },
                    new() { 4, 5 },
                    new() { 6 }
                });
            var result = combos.Select(c => c.ToArray()).ToArray();

            result[0].ShouldBe(new[] { 1, 4, 6 });
            result[1].ShouldBe(new[] { 1, 5, 6 });

            result[2].ShouldBe(new[] { 2, 4, 6 });
            result[3].ShouldBe(new[] { 2, 5, 6 });
        }
    }
}