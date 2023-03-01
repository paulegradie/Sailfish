using System;
using System.Threading.Tasks;
using Accord.Collections;
using Sailfish;
using Sailfish.Analysis;
using Shouldly;
using Tests.E2ETestSuite;
using Xunit;

namespace Test.Execution;

public class FullE2EFixture
{
    [Fact]
    public async Task AFullTestRunOfTheDemoDoesNotContainErrors()
    {
        var runSettings = new RunSettings(
            Array.Empty<string>(),
            string.Empty,
            string.Empty,
            false,
            true,
            true,
            new TestSettings(0.01, 2, true),
            new OrderedDictionary<string, string>(),
            new OrderedDictionary<string, string>(),
            string.Empty,
            DateTime.Now,
            new[] { typeof(E2ETestRegistrationProvider) },
            new[] { typeof(E2ETestRegistrationProvider) }
        );


        var result = await SailfishRunner.Run(runSettings);
        ;

        result.IsValid.ShouldBe(true);
        result.Exceptions.ShouldBeNull();
        

    }
}