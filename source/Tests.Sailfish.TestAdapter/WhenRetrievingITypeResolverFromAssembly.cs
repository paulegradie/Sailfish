using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sailfish.Execution;
using Sailfish.TestAdapter.Utils;
using Shouldly;

namespace Tests.Sailfish.TestAdapter;

[TestClass]
public class WhenRetrievingITypeResolverFromAssembly
{
    [TestMethod]
    public void SailfishTypeProviderIsFound()
    {
        var assembly = typeof(WhenRetrievingITypeResolverFromAssembly).Assembly;
        var provider = assembly.GetTypeResolverOrNull();

        provider.ShouldNotBeNull();
        provider.GetType().ShouldBe(typeof(MyProvider));
    }
}

public class MyProvider : SailfishTypeProvider
{
    public override object ResolveType(Type type)
    {
        return new MyProvider();
    }
}