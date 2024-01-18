using Sailfish.Attributes;
using System;

namespace Tests.TestAdapter.TestResources.NamespaceOne;

[Sailfish]
public class DuplicateTest
{
    [SailfishMethod]
    public void Duplicate()
    {
        Console.WriteLine();
    }
}