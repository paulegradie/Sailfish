using System;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class TypeActivatorTests
{
    [Sailfish]
    private class TestClassWithDependency
    {
        private readonly INotRegistered _dependency;

        public TestClassWithDependency(INotRegistered dependency)
        {
            _dependency = dependency;
        }

        [SailfishMethod]
        public void Method() { _ = _dependency; }
    }

    [Sailfish]
    private class TestClassThatThrowsInCtor
    {
        public TestClassThatThrowsInCtor()
        {
            throw new InvalidOperationException("boom from ctor");
        }

        [SailfishMethod]
        public void Method() { }
    }

    private interface INotRegistered { }

    private static IServiceProvider EmptyProvider()
    {
        return new ServiceCollection().BuildServiceProvider();
    }

    [Fact]
    public void CreateDehydratedTestInstance_WhenDependencyIsNotRegistered_ThrowsTestClassInstantiationException()
    {
        var provider = EmptyProvider();
        var activator = new TypeActivator(provider);

        var ex = Should.Throw<TestClassInstantiationException>(() =>
            activator.CreateDehydratedTestInstance(
                typeof(TestClassWithDependency),
                new TestCaseId("TestClassWithDependency.Method"),
                disabled: false));

        ex.TestType.ShouldBe(typeof(TestClassWithDependency));
        // The new MS-DI-based TypeActivator throws SailfishException (instead of Autofac's
        // ComponentNotRegisteredException) when a constructor dependency cannot be resolved, then wraps it
        // in TestClassInstantiationException — matching the historical outer-exception contract.
        ex.InnerException.ShouldBeOfType<SailfishException>();
        ex.Message.ShouldContain(typeof(TestClassWithDependency).FullName!);
    }

    [Fact]
    public void CreateDehydratedTestInstance_WhenConstructorThrows_ThrowsTestClassInstantiationException_UnwrappingTargetInvocationException()
    {
        var provider = EmptyProvider();
        var activator = new TypeActivator(provider);

        var ex = Should.Throw<TestClassInstantiationException>(() =>
            activator.CreateDehydratedTestInstance(
                typeof(TestClassThatThrowsInCtor),
                new TestCaseId("TestClassThatThrowsInCtor.Method"),
                disabled: false));

        ex.TestType.ShouldBe(typeof(TestClassThatThrowsInCtor));
        ex.InnerException.ShouldBeOfType<InvalidOperationException>();
        ex.InnerException!.Message.ShouldBe("boom from ctor");
    }
}
