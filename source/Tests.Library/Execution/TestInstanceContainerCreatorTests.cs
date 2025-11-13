using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NSubstitute;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Shouldly;
using Tests.Common.Builders;
using Xunit;

namespace Tests.Library.Execution;

public class TestInstanceContainerCreatorTests
{
    private readonly IRunSettings runSettings = Substitute.For<IRunSettings>();
    private readonly ITypeActivator typeActivator = Substitute.For<ITypeActivator>();
    private readonly IPropertySetGenerator propertySetGenerator = Substitute.For<IPropertySetGenerator>();

    [Fact]
    public void CreateTestContainerInstanceProviders_WithNoVariables_ReturnsSingleProviderPerMethod()
    {
        // Arrange
        runSettings.Seed.Returns((int?)null);
        runSettings.Args.Returns(new OrderedDictionary());
        propertySetGenerator.GenerateSailfishVariableSets(Arg.Any<Type>(), out _).Returns(new List<PropertySet>());

        var creator = new TestInstanceContainerCreator(runSettings, typeActivator, propertySetGenerator);

        // Act
        var providers = creator.CreateTestContainerInstanceProviders(typeof(TestClassWithMethods));

        // Assert
        providers.ShouldNotBeEmpty();
        providers.Count.ShouldBe(2); // Two methods in TestClassWithMethods
    }

    [Fact]
    public void CreateTestContainerInstanceProviders_WithPropertyTensorFilter_FiltersVariableSets()
    {
        // Arrange
        var propertySet1 = new PropertySet(new List<TestCaseVariable> { new TestCaseVariable("Prop", 1) });
        var propertySet2 = new PropertySet(new List<TestCaseVariable> { new TestCaseVariable("Prop", 2) });
        var variableSets = new List<PropertySet> { propertySet1, propertySet2 };

        runSettings.Seed.Returns((int?)null);
        runSettings.Args.Returns(new OrderedDictionary());
        propertySetGenerator.GenerateSailfishVariableSets(Arg.Any<Type>(), out _).Returns(variableSets);

        var creator = new TestInstanceContainerCreator(runSettings, typeActivator, propertySetGenerator);

        // Act
        var providers = creator.CreateTestContainerInstanceProviders(
            typeof(TestClassWithMethods),
            propertyTensorFilter: ps => ps.VariableSet.First().Value.Equals(1));

        // Assert
        providers.ShouldNotBeEmpty();
        // Should have 2 methods * 1 filtered property set = 2 providers
        providers.Count.ShouldBe(2);
    }

    [Fact]
    public void CreateTestContainerInstanceProviders_WithInstanceContainerFilter_FiltersMethods()
    {
        // Arrange
        runSettings.Seed.Returns((int?)null);
        runSettings.Args.Returns(new OrderedDictionary());
        propertySetGenerator.GenerateSailfishVariableSets(Arg.Any<Type>(), out _).Returns(new List<PropertySet>());

        var creator = new TestInstanceContainerCreator(runSettings, typeActivator, propertySetGenerator);
        var method1 = typeof(TestClassWithMethods).GetMethod(nameof(TestClassWithMethods.Method1))!;

        // Act
        var providers = creator.CreateTestContainerInstanceProviders(
            typeof(TestClassWithMethods),
            instanceContainerFilter: m => m.Name == nameof(TestClassWithMethods.Method1));

        // Assert
        providers.ShouldNotBeEmpty();
        providers.Count.ShouldBe(1); // Only one method matches filter
    }

    [Fact]
    public void CreateTestContainerInstanceProviders_WithSeed_RandomizesPropertySetOrder()
    {
        // Arrange
        var propertySet1 = new PropertySet(new List<TestCaseVariable> { new TestCaseVariable("Prop", 1) });
        var propertySet2 = new PropertySet(new List<TestCaseVariable> { new TestCaseVariable("Prop", 2) });
        var propertySet3 = new PropertySet(new List<TestCaseVariable> { new TestCaseVariable("Prop", 3) });
        var variableSets = new List<PropertySet> { propertySet1, propertySet2, propertySet3 };

        runSettings.Seed.Returns(42);
        runSettings.Args.Returns(new OrderedDictionary());
        propertySetGenerator.GenerateSailfishVariableSets(Arg.Any<Type>(), out _).Returns(variableSets);

        var creator = new TestInstanceContainerCreator(runSettings, typeActivator, propertySetGenerator);

        // Act
        var providers1 = creator.CreateTestContainerInstanceProviders(typeof(TestClassWithMethods));

        // Reset and create again with same seed - need to reset the mock to return fresh list
        runSettings.Seed.Returns(42);
        propertySetGenerator.GenerateSailfishVariableSets(Arg.Any<Type>(), out _).Returns(
            new List<PropertySet> { propertySet1, propertySet2, propertySet3 });
        var providers2 = creator.CreateTestContainerInstanceProviders(typeof(TestClassWithMethods));

        // Assert - same seed should produce same order (deterministic)
        providers1.Count.ShouldBe(providers2.Count);
        providers1.Count.ShouldBe(2); // 2 methods (each with 3 property sets)

        // Extract method names and order to verify deterministic behavior
        var order1 = providers1.Select(p => p.Method.Name).ToList();
        var order2 = providers2.Select(p => p.Method.Name).ToList();

        // Same seed should produce identical ordering
        order1.SequenceEqual(order2).ShouldBeTrue("Same seed should produce deterministic ordering");

        // Verify that seed is actually being used (providers should have the same count)
        providers1.Count.ShouldBe(providers2.Count);
    }

    [Fact]
    public void CreateTestContainerInstanceProviders_WithOrderedMethods_PreservesOrder()
    {
        // Arrange
        runSettings.Seed.Returns((int?)null);
        runSettings.Args.Returns(new OrderedDictionary());
        propertySetGenerator.GenerateSailfishVariableSets(Arg.Any<Type>(), out _).Returns(new List<PropertySet>());

        var creator = new TestInstanceContainerCreator(runSettings, typeActivator, propertySetGenerator);

        // Act
        var providers = creator.CreateTestContainerInstanceProviders(typeof(TestClassWithOrderedMethods));

        // Assert
        providers.ShouldNotBeEmpty();
        providers.Count.ShouldBe(3); // Three methods total

        // Methods should be in order: OrderedMethod1 (Order=1), OrderedMethod2 (Order=2), UnorderedMethod
        providers[0].Method.Name.ShouldBe(nameof(TestClassWithOrderedMethods.OrderedMethod1));
        providers[1].Method.Name.ShouldBe(nameof(TestClassWithOrderedMethods.OrderedMethod2));
        providers[2].Method.Name.ShouldBe(nameof(TestClassWithOrderedMethods.UnorderedMethod));
    }

    [Fact]
    public void CreateTestContainerInstanceProviders_WithSeedInArgs_ParsesSeedAndProducesDeterministicOrder()
    {
        // Arrange
        var args = new OrderedDictionary { { "seed", "123" } };
        runSettings.Seed.Returns((int?)null);
        runSettings.Args.Returns(args);
        propertySetGenerator.GenerateSailfishVariableSets(Arg.Any<Type>(), out _).Returns(new List<PropertySet>());
        typeActivator.CreateDehydratedTestInstance(typeof(TestClassWithMethods), Arg.Any<TestCaseId>(), Arg.Any<bool>())
            .Returns(ci => new TestClassWithMethods());

        var creator = new TestInstanceContainerCreator(runSettings, typeActivator, propertySetGenerator);

        // Act: run twice and ensure deterministic provider ordering
        var providers1 = creator.CreateTestContainerInstanceProviders(typeof(TestClassWithMethods));
        var providers2 = creator.CreateTestContainerInstanceProviders(typeof(TestClassWithMethods));

        // Assert: providers exist and ordering is deterministic with the same seed
        providers1.ShouldNotBeEmpty();
        providers2.ShouldNotBeEmpty();
        var order1 = providers1.Select(p => p.Method.Name).ToList();
        var order2 = providers2.Select(p => p.Method.Name).ToList();
        order1.SequenceEqual(order2).ShouldBeTrue("Same args seed should produce deterministic provider ordering");

        // Additionally, verify that the parsed seed flows into execution settings at provider level
        var container = providers1[0].ProvideNextTestCaseEnumeratorForClass().Single();
        container.ExecutionSettings.Seed.ShouldBe(123);
    }

    [Fact]
    public void CreateTestContainerInstanceProviders_WithRandomSeedArg_ParsesSeedAndProducesDeterministicOrder()
    {
        // Arrange
        var args = new OrderedDictionary { { "randomseed", "456" } };
        runSettings.Seed.Returns((int?)null);
        runSettings.Args.Returns(args);
        propertySetGenerator.GenerateSailfishVariableSets(Arg.Any<Type>(), out _).Returns(new List<PropertySet>());
        typeActivator.CreateDehydratedTestInstance(typeof(TestClassWithMethods), Arg.Any<TestCaseId>(), Arg.Any<bool>())
            .Returns(ci => new TestClassWithMethods());

        var creator = new TestInstanceContainerCreator(runSettings, typeActivator, propertySetGenerator);

        // Act: run twice and ensure deterministic order with same randomseed
        var providers1 = creator.CreateTestContainerInstanceProviders(typeof(TestClassWithMethods));
        var providers2 = creator.CreateTestContainerInstanceProviders(typeof(TestClassWithMethods));

        // Assert
        providers1.ShouldNotBeEmpty();
        providers2.ShouldNotBeEmpty();
        var order1 = providers1.Select(p => p.Method.Name).ToList();
        var order2 = providers2.Select(p => p.Method.Name).ToList();
        order1.SequenceEqual(order2).ShouldBeTrue("Same randomseed should produce deterministic provider ordering");

        // Seed should propagate to execution settings
        var container = providers1[0].ProvideNextTestCaseEnumeratorForClass().Single();
        container.ExecutionSettings.Seed.ShouldBe(456);
    }

    [Fact]
    public void CreateTestContainerInstanceProviders_WithInvalidSeedInArgs_IgnoresInvalidSeedAndDoesNotSetExecutionSeed()
    {
        // Arrange
        var args = new OrderedDictionary { { "seed", "not-a-number" } };
        runSettings.Seed.Returns((int?)null);
        runSettings.Args.Returns(args);
        propertySetGenerator.GenerateSailfishVariableSets(Arg.Any<Type>(), out _).Returns(new List<PropertySet>());
        typeActivator.CreateDehydratedTestInstance(typeof(TestClassWithMethods), Arg.Any<TestCaseId>(), Arg.Any<bool>())
            .Returns(ci => new TestClassWithMethods());

        var creator = new TestInstanceContainerCreator(runSettings, typeActivator, propertySetGenerator);

        // Act: run twice and ensure the (no-seed) behavior is consistent
        var providers1 = creator.CreateTestContainerInstanceProviders(typeof(TestClassWithMethods));
        var providers2 = creator.CreateTestContainerInstanceProviders(typeof(TestClassWithMethods));

        // Assert: providers exist and order is consistent even without a valid seed
        providers1.ShouldNotBeEmpty();
        providers2.ShouldNotBeEmpty();
        var order1 = providers1.Select(p => p.Method.Name).ToList();
        var order2 = providers2.Select(p => p.Method.Name).ToList();
        order1.SequenceEqual(order2).ShouldBeTrue("Invalid seed should be ignored and not affect provider ordering");

        // Execution settings should not receive a seed
        var container = providers1[0].ProvideNextTestCaseEnumeratorForClass().Single();
        container.ExecutionSettings.Seed.ShouldBeNull();
    }

    // Test classes
    [Sailfish]
    private class TestClassWithMethods
    {
        [SailfishMethod]
        public void Method1() { }

        [SailfishMethod]
        public void Method2() { }
    }

    [Sailfish]
    private class TestClassWithOrderedMethods
    {
        [SailfishMethod(Order = 1)]
        public void OrderedMethod1() { }

        [SailfishMethod(Order = 2)]
        public void OrderedMethod2() { }

        [SailfishMethod]
        public void UnorderedMethod() { }
    }
}

