using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Sailfish.Utilities;
using Shouldly;
using Xunit;

namespace Tests.Library.Utilities;

public class ConsumerTests
{
    [Fact]
    public void Consume_Accepts_Reference_And_Value_Types_Without_Exception()
    {
        // Reference type
        Consumer.Consume("hello");

        // Value type
        Consumer.Consume(42);

        // Nullable value type
        int? maybe = 7;
        Consumer.Consume(maybe);

        // Anonymous object
        Consumer.Consume(new { X = 1, Y = 2 });
    }

    [Fact]
    public void Consume_Method_Is_Marked_NoInlining()
    {
        var mi = typeof(Consumer).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(Consumer.Consume) && m.IsGenericMethodDefinition);

        var flags = mi.GetMethodImplementationFlags();
        (flags & MethodImplAttributes.NoInlining).ShouldBe(MethodImplAttributes.NoInlining);
    }
}