using System;
using Sailfish.Attributes;

namespace Tests.TestAdapter.TestResources.NamespaceOne;

[Sailfish(Disabled = true)]
public class DuplicateTest
{
    [SailfishMethod]
    public void Duplicate()
    {
        Console.WriteLine();
    }
}