using System.Linq;
using System.Threading.Tasks;
using Sailfish.Analysis;
using Sailfish.Utils;
using Shouldly;
using Xunit;

namespace Test.Utils.DisplayNames;

public class WhenCreatingTestCaseIdsWithASingleVariable : IAsyncLifetime
{
    private TestCaseId testCaseId = null!;
    private const string VariableName = "YoMamma";
    private const string MethodName = "TestMethod";
    private const int Param = 1;

    [Fact]
    public void DisplayNameIsFormedCorrectly()
    {
        testCaseId.DisplayName.ShouldBe($"{nameof(WhenCreatingTestCaseIdsWithASingleVariable)}.{MethodName}({VariableName}: {Param})");
    }

    [Fact]
    public void NamePropertyOfTestCaseNameIsCorrect()
    {
        testCaseId.TestCaseName.Name.ShouldBe($"{nameof(WhenCreatingTestCaseIdsWithASingleVariable)}.{MethodName}");
    }

    [Fact]
    public void PartsPropertyOfTestCaseNameIsCorrect()
    {
        testCaseId.TestCaseName.Parts.ShouldBeEquivalentTo(new[] { nameof(WhenCreatingTestCaseIdsWithASingleVariable), MethodName });
    }

    [Fact]
    public void VariablesPropertyOfTestCaseVariablesShouldBeCorrect()
    {
        var variables = testCaseId.TestCaseVariables.Variables.ToList();
        variables.Count.ShouldBe(1);
        variables.Single().Name.ShouldBe(VariableName);
        variables.Single().Value.ShouldBe(1);
    }

    [Fact]
    public void TestCaseVariablesFormVariableSectionShouldBeCorrect()
    {
        testCaseId.TestCaseVariables.FormVariableSection().ShouldBe($"({VariableName}: {Param})");
    }

    [Fact]
    public void TestCaseVariablesGetVariableIndexShouldBeCorrect()
    {
        var result = testCaseId.TestCaseVariables.GetVariableIndex(0);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(VariableName);
        result.Value.ShouldBe(1);
    }

    [Fact]
    public void TestCaseVariablesGetVariableIndexShouldBeNullWhenNonExist()
    {
        var result = testCaseId.TestCaseVariables.GetVariableIndex(1);
        result.ShouldBeNull();
    }

    [Fact]
    public void TheTestCaseDisplayNameIsRendered()
    {
        var varA = "First";
        var varB = "Second";
        var tci = DisplayNameHelper.CreateTestCaseId(
            typeof(WhenCreatingTestCaseIdsWithASingleVariable),
            "TestMethod",
            new[] { varA, varB },
            new object[] { 10, 50 });
        
        var result = tci.TestCaseVariables.FormVariableSection();
        result.ShouldBe($"({varA}: 10, {varB}: 50)");
    }

    public async Task InitializeAsync()
    {
        testCaseId = DisplayNameHelper.CreateTestCaseId(
            typeof(WhenCreatingTestCaseIdsWithASingleVariable),
            "TestMethod",
            new[] { VariableName },
            new object[] { Param });
        await Task.Yield();
    }

    public async Task DisposeAsync()
    {
        await Task.Yield();
    }
}