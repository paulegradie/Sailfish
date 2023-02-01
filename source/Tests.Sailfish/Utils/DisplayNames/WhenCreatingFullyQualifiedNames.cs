using Sailfish.Utils;
using Shouldly;
using Xunit;

namespace Test.Utils.DisplayNames;

public class WhenCreatingFullyQualifiedNames
{
    [Fact]
    public void WithNoVariablesTheFullyQualifiedNameShouldBeCorrect()
    {
        var result = DisplayNameHelper.FullyQualifiedName(typeof(WhenCreatingFullyQualifiedNames), nameof(WithNoVariablesTheFullyQualifiedNameShouldBeCorrect));
        result.ShouldBe(
            $"{typeof(WhenCreatingFullyQualifiedNames).Namespace}.{nameof(WhenCreatingFullyQualifiedNames)}.{nameof(WithNoVariablesTheFullyQualifiedNameShouldBeCorrect)}()");
    }

    [Fact]
    public void WithVariablesNameShouldIncludeVariableTypes()
    {
        var result = DisplayNameHelper.FullyQualifiedName(typeof(WhenCreatingFullyQualifiedNames), nameof(HelperMethod));
        result.ShouldBe($"{typeof(WhenCreatingFullyQualifiedNames).Namespace}.{nameof(WhenCreatingFullyQualifiedNames)}.{nameof(HelperMethod)}(Int32, Int32)");
    }

#pragma warning disable xUnit1013
#pragma warning disable CA1822
    public void HelperMethod(int x, int y)
#pragma warning restore CA1822
#pragma warning restore xUnit1013
    {
    }
}