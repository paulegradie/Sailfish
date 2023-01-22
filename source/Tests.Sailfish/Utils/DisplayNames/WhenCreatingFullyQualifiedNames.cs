using System.Diagnostics.CodeAnalysis;
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

    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public void HelperMethod(int x, int y)
    {
    }
}