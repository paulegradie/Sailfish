using System;
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
        var result = typeof(TestSailfishTest).GetSailfishFixtureGenericArguments();
        result.ShouldNotBeNull();
    }

    [Fact]
    public void NullIsReturnedWhenTheTypeDoesNotImplementISailfishFixtureDependency()
    {
        var result = typeof(AnyType).GetSailfishFixtureGenericArguments();
        result.ShouldBeEmpty();
    }

    // ReSharper disable once ClassNeverInstantiated.Local
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

        public T ResolveType<T>() where T : notnull
        {
            return Activator.CreateInstance<T>();
        }

        public void Dispose()
        {
        }
    }
}