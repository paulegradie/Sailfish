using System;
using Sailfish.Attributes;

namespace Tests.TestAdapter.TestResources.NamespaceTwo;

[Sailfish]
public class DuplicateTest
{
    [SailfishMethod]
    public void Duplicate()
    {
        Console.WriteLine();
    }
}