using System.Collections.Generic;
using System.Linq;
using Shouldly;
using VeerPerforma.Executor;
using VeerPerforma.Executor.Prep;
using Xunit;

namespace Test.VeerPerforma.Executor.Tests;

public class WhenCompilingIterationVariables
{
    [Fact]
    public void AllCombinationsAreFound_TwoProperties()
    {
        var combos = InstanceConstructor.GetAllPossibleCombos(
            new List<List<string>>
            {
                new List<string> { "a", "b", "c" },
                new List<string> { "x", "y" },
                new List<string> { "1", "2", "3", "4" }
            });
        var result = combos.Select(c => c.ToArray()).ToArray();

        result.Length.ShouldBe(24);
    }
}