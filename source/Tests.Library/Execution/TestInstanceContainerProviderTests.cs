using System.Linq;
using System.Reflection;
using NSubstitute;
using Shouldly;
using Xunit;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;

namespace Tests.Library.Execution;

public class TestInstanceContainerProviderTests
{
    [Sailfish]
    private class ProviderTestClass
    {
        public int Value { get; set; }

        [SailfishMethod]
        public void MethodUnderTest() { }
    }

    [Sailfish(Disabled = true)]
    private class DisabledClass
    {
        [SailfishMethod]
        public void MethodUnderTest() { }
    }

    private static IRunSettings CreateRunSettings()
    {
        var rs = Substitute.For<IRunSettings>();
        rs.SampleSizeOverride.Returns((int?)null);
        rs.NumWarmupIterationsOverride.Returns((int?)null);
        rs.GlobalUseAdaptiveSampling.Returns((bool?)null);
        rs.GlobalTargetCoefficientOfVariation.Returns((double?)null);
        rs.GlobalMaximumSampleSize.Returns((int?)null);
        return rs;
    }

    private static MethodInfo GetMethod<T>(string name) => typeof(T).GetMethod(name)!;

    [Fact]
    public void ProvideNextTestCaseEnumeratorForClass_NoPropertySets_YieldsSingleContainer_WithDisabledFalse()
    {
        var runSettings = CreateRunSettings();
        var typeActivator = Substitute.For<ITypeActivator>();

        var method = GetMethod<ProviderTestClass>(nameof(ProviderTestClass.MethodUnderTest));
        var provider = new TestInstanceContainerProvider(runSettings, typeActivator, typeof(ProviderTestClass), [], method);

        // Will be called to create the instance
        typeActivator.CreateDehydratedTestInstance(typeof(ProviderTestClass), Arg.Any<TestCaseId>(), Arg.Any<bool>())
            .Returns(ci => new ProviderTestClass());

        var containers = provider.ProvideNextTestCaseEnumeratorForClass().ToList();

        containers.Count.ShouldBe(1);
        var c = containers.Single();
        c.Type.ShouldBe(typeof(ProviderTestClass));
        c.ExecutionMethod.Name.ShouldBe(nameof(ProviderTestClass.MethodUnderTest));
        c.Disabled.ShouldBeFalse();
        c.ExecutionSettings.ShouldNotBeNull();
    }

    [Fact]
    public void ProvideNextTestCaseEnumeratorForClass_WithPropertySets_HydratesProperties()
    {
        var runSettings = CreateRunSettings();
        var typeActivator = Substitute.For<ITypeActivator>();

        var ps = new PropertySet([new("Value", 42)]);
        var method = GetMethod<ProviderTestClass>(nameof(ProviderTestClass.MethodUnderTest));
        var provider = new TestInstanceContainerProvider(runSettings, typeActivator, typeof(ProviderTestClass), [ps], method);

        ProviderTestClass? capturedInstance = null;
        typeActivator.CreateDehydratedTestInstance(typeof(ProviderTestClass), Arg.Any<TestCaseId>(), Arg.Any<bool>())
            .Returns(ci =>
            {
                var inst = new ProviderTestClass();
                capturedInstance = inst;
                return inst;
            });

        var containers = provider.ProvideNextTestCaseEnumeratorForClass().ToList();
        containers.Count.ShouldBe(1);
        capturedInstance.ShouldNotBeNull();
        capturedInstance!.Value.ShouldBe(42);
    }

    [Fact]
    public void ProvideNextTestCaseEnumeratorForClass_DisabledClass_SetsDisabledTrue()
    {
        var runSettings = CreateRunSettings();
        var typeActivator = Substitute.For<ITypeActivator>();

        var method = GetMethod<DisabledClass>(nameof(DisabledClass.MethodUnderTest));
        var provider = new TestInstanceContainerProvider(runSettings, typeActivator, typeof(DisabledClass), [], method);

        typeActivator.CreateDehydratedTestInstance(typeof(DisabledClass), Arg.Any<TestCaseId>(), Arg.Any<bool>())
            .Returns(ci => new DisabledClass());

        var container = provider.ProvideNextTestCaseEnumeratorForClass().Single();
        container.Disabled.ShouldBeTrue();
    }

    [Fact]
    public void GetNumberOfPropertySetsInTheQueue_ReturnsExpectedCount()
    {
        var runSettings = CreateRunSettings();
        var typeActivator = Substitute.For<ITypeActivator>();
        var method = GetMethod<ProviderTestClass>(nameof(ProviderTestClass.MethodUnderTest));
        var sets = new[]
        {
            new PropertySet([new("Value", 1)]),
            new PropertySet([new("Value", 2)])
        };
        var provider = new TestInstanceContainerProvider(runSettings, typeActivator, typeof(ProviderTestClass), sets, method);
        provider.GetNumberOfPropertySetsInTheQueue().ShouldBe(2);
    }
}

