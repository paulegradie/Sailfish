using Sailfish.Analysis;
using Sailfish.Attributes;
using Shouldly;
using Tests.E2E.TestSuite.Utils;

namespace Tests.E2E.TestSuite.Discoverable;

[Sailfish(3, 0, Disabled = Constants.Disabled)]
public class ResolveTestCaseIdTest
{
    private readonly TestCaseId testCaseId;

    public ResolveTestCaseIdTest(TestCaseId testCaseId)
    {
        this.testCaseId = testCaseId;
    }
    [SailfishMethod]
    public void MainMethod()
    {
        testCaseId.DisplayName.ShouldBe($"{nameof(ResolveTestCaseIdTest)}.{nameof(MainMethod)}()");
    }
}

[Sailfish]
public class ResolveTestCaseIdTestMultipleCtorArgs
{
    private readonly TestCaseId testCaseId;

    public ResolveTestCaseIdTestMultipleCtorArgs(ExampleDependencyForAltRego dep, TestCaseId testCaseId)
    {
        this.testCaseId = testCaseId;
    }
    [SailfishMethod]
    public void MainMethod()
    {
        testCaseId.DisplayName.ShouldBe($"{nameof(ResolveTestCaseIdTestMultipleCtorArgs)}.{nameof(MainMethod)}()");
    }
}

