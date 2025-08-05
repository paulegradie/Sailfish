﻿using Sailfish.Attributes;
using Shouldly;
using System.Collections.Generic;

namespace Tests.E2E.TestSuite.Discoverable;

[Sailfish(1, 0, Disabled = Constants.Disabled)]
public class VariablesIterateTest
{
    private static List<int> Expected = new();

    private string FieldMan = null!;

    [SailfishVariable(1, 2)]
    public int N { get; set; }

    public int MyInt { get; set; } = 456;

    [SailfishGlobalSetup]
    public void Setup()
    {
        FieldMan = "WOW";
    }

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        MyInt = 789;
    }

    [SailfishMethod]
    public void Increment()
    {
        FieldMan.ShouldBe("WOW");
        Expected.Add(N);
    }

    [SailfishMethod]
    public void SecondIncrement()
    {
        MyInt.ShouldBe(789);
        FieldMan.ShouldBe("WOW");
        Expected.Add(N);
    }

    [SailfishGlobalTeardown]
    public void GlobalTeardownAssertions()
    {
        FieldMan.ShouldBe("WOW");
        Expected.ShouldBe(new List<int> { 1, 2, 1, 2 });
        Expected = new List<int>();
    }
}