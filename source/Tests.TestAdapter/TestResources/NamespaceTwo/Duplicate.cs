using Sailfish.Attributes;
using System;

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