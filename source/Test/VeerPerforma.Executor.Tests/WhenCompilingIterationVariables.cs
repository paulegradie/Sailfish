using System.Collections.Generic;
using System.Linq;
using Shouldly;
using VeerPerforma.Executor.Prep;
using Xunit;

namespace Test.VeerPerforma.Executor.Tests;

public class WhenCompilingIterationVariables
{
    [Fact]
    public void AllCombinationsAreFound_TwoProperties()
    {
        var combinator = new ParameterCombinationMaker();
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
}