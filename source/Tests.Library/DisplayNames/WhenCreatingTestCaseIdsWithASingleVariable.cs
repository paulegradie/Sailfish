using System.Linq;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Models;
using Sailfish.Utils;
using Shouldly;
using Xunit;

namespace Tests.Library.DisplayNames;

public class WhenCreatingTestCaseIdsWithASingleVariable : IAsyncLifetime
{
    private const string VariableName = "YoMamma";
    private const string MethodName = "TestMethod";
    private const int Param = 1;
    private TestCaseId _testCaseId = null!;

    public async Task InitializeAsync()
    {
        _testCaseId = DisplayNameHelper.CreateTestCaseId(
            typeof(WhenCreatingTestCaseIdsWithASingleVariable),
            "TestMethod",
            [VariableName],
            [Param]);
        await Task.Yield();
    }

    public async Task DisposeAsync()
    {
        await Task.Yield();
    }

    [Fact]
    public void DisplayNameIsFormedCorrectly()
    {
        _testCaseId.DisplayName.ShouldBe($"{nameof(WhenCreatingTestCaseIdsWithASingleVariable)}.{MethodName}({VariableName}: {Param})");
    }

    [Fact]
    public void NamePropertyOfTestCaseNameIsCorrect()
    {
        _testCaseId.TestCaseName.Name.ShouldBe($"{nameof(WhenCreatingTestCaseIdsWithASingleVariable)}.{MethodName}");
    }

    [Fact]
    public void PartsPropertyOfTestCaseNameIsCorrect()
    {
        _testCaseId.TestCaseName.Parts.ShouldBeEquivalentTo(new[] { nameof(WhenCreatingTestCaseIdsWithASingleVariable), MethodName });
    }

    [Fact]
    public void VariablesPropertyOfTestCaseVariablesShouldBeCorrect()
    {
        var variables = _testCaseId.TestCaseVariables.Variables.ToList();
        variables.Count.ShouldBe(1);
        variables.Single().Name.ShouldBe(VariableName);
        variables.Single().Value.ShouldBe(1);
    }

    [Fact]
    public void TestCaseVariablesFormVariableSectionShouldBeCorrect()
    {
        _testCaseId.TestCaseVariables.FormVariableSection().ShouldBe($"({VariableName}: {Param})");
    }

    [Fact]
    public void TestCaseVariablesGetVariableIndexShouldBeCorrect()
    {
        var result = _testCaseId.TestCaseVariables.GetVariableIndex(0);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(VariableName);
        result.Value.ShouldBe(1);
    }

    [Fact]
    public void TestCaseVariablesGetVariableIndexShouldBeNullWhenNonExist()
    {
        var result = _testCaseId.TestCaseVariables.GetVariableIndex(1);
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
            [varA, varB],
            [10, 50]);

        var result = tci.TestCaseVariables.FormVariableSection();
        result.ShouldBe($"({varA}: 10, {varB}: 50)");
    }
}