using System;
using System.Collections.Generic;
using System.Reflection;
using NSubstitute;
using Sailfish.AdapterUtils;
using Sailfish.Attributes;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Test.Execution;

public class WhenCreatingContainers
{
    [Fact]
    public void TheSailfishDependencyFixtureDependenciesAreFound()
    {
        var testContainerCreator = new TestInstanceContainerProvider(
            null,
            typeof(TestSailfishTest),
            new List<int[]>() { new[] { 1 } }.ToArray(),
            new List<string>() { "VariableA" },
            Substitute.For<MethodInfo>()
        );

        var result = testContainerCreator.GetSailfishFixtureGenericArgument();
        result.ShouldNotBeNull();

        var testDep = result.ResolveType(typeof(AnyType));
        testDep.GetType().ShouldBe(typeof(TestDependencies));
    }

    private class AnyType
    {
    }

    [Sailfish(NumIterations = 3, NumWarmupIterations = 2)]
    public class TestSailfishTest : TestBase
    {
        [SailfishVariable(1, 2, 3)] public int VariableA { get; set; }

        [SailfishMethod]
        public void Go()
        {
        }

        public TestSailfishTest(TestDependencies testDeps) : base(testDeps)
        {
        }
    }

    public abstract class TestBase : ISailfishFixture<TestDependencies>
    {
        private readonly TestDependencies testDeps;

        public TestBase(TestDependencies testDeps)
        {
            this.testDeps = testDeps;
        }
    }

    public class TestDependencies : ISailfishFixtureDependency
    {
        public object ResolveType(Type type)
        {
            return new TestDependencies();
        }

        public void Dispose()
        {
        }
    }
}