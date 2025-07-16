using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using System;
using System.Collections.Generic;

namespace PerformanceTests.ExamplePerformanceTests;

[Sailfish(NumWarmupIterations = 0)]
public class StructuredObjectVariableExample
{
    public required ITestParams TestParams { get; set; }

    [SailfishMethod]
    public void TestMethod()
    {
        Console.WriteLine(TestParams.A);
        Console.WriteLine(TestParams.B);
        Console.WriteLine(TestParams.Inner.C);
        Console.WriteLine(TestParams.Inner.D);
    }
}

// user defines this interface and sets it as the property type
public interface ITestParams : ISailfishComplexVariableProvider<TestParams>
{
    public int A { get; init; }
    public string B { get; init; }
    public InnerParam Inner { get; init; }
}

// user implements the provider interface
public record TestParams(int A, string B, InnerParam Inner) : ITestParams
{
    public int CompareTo(object? obj)
    {
        if (obj is not TestParams other) return 1;

        var aComparison = A.CompareTo(other.A);
        if (aComparison != 0) return aComparison;

        var bComparison = string.Compare(B, other.B, StringComparison.Ordinal);
        if (bComparison != 0) return bComparison;

        return Inner.C.CompareTo(other.Inner.C);
    }

    public static IEnumerable<TestParams> GetVariableInstances()
    {
        return
        [
            new TestParams(1, "A", new InnerParam(1.1, 1.2m)),
            new TestParams(2, "B", new InnerParam(2.1, 2.2m)),
            new TestParams(3, "C", new InnerParam(3.1, 3.2m))
        ];
    }
}

public record InnerParam(double C, decimal D);