using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Tests.E2E.TestSuite.Utils;

namespace Tests.E2E.TestSuite.Discoverable;

// Injects TestCaseId, which identifies a specific case — inherently per-case, so this class opts into PerCase
// lifetime (under the default SharedInstance, the constructor runs once and there is no single case id).
[Sailfish(3, 0, Disabled = Constants.Disabled, Lifetime = SailfishLifetime.PerCase)]
public class ResolveTestCaseIdTest
{
    private readonly TestCaseId _testCaseId;

    public ResolveTestCaseIdTest(TestCaseId testCaseId)
    {
        _testCaseId = testCaseId;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        _testCaseId.DisplayName.ShouldBe($"{nameof(ResolveTestCaseIdTest)}.{nameof(MainMethod)}()");
    }
}

[Sailfish(Lifetime = SailfishLifetime.PerCase)]
public class ResolveTestCaseIdTestMultipleCtorArgs
{
    private readonly TestCaseId _testCaseId;

    public ResolveTestCaseIdTestMultipleCtorArgs(ExampleDependencyForAltRego dep, TestCaseId testCaseId)
    {
        _testCaseId = testCaseId;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        _testCaseId.DisplayName.ShouldBe($"{nameof(ResolveTestCaseIdTestMultipleCtorArgs)}.{nameof(MainMethod)}()");
    }
}